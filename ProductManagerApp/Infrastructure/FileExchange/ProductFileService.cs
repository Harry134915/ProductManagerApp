using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using ProductManagerApp.BLL.Validators;
using ProductManagerApp.DTO;
using System.Globalization;
using System.IO;
using System.Text;

namespace ProductManagerApp.Infrastructure.FileExchange
{
    /// <summary>
    /// 使用稳定的结构化解析器读写商品 CSV/XLSX 文件。
    /// </summary>
    public sealed class ProductFileService : IProductFileService
    {
        private static readonly string[] Headers =
        {
            "商品编码", "商品名称", "价格", "库存", "描述"
        };

        public ProductImportReadResult ReadImport(string path, ProductFileFormat format)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            ProductImportReadResult result;
            try
            {
                result = format switch
                {
                    ProductFileFormat.Csv => ReadCsv(path),
                    ProductFileFormat.Xlsx => ReadXlsx(path),
                    _ => throw new ArgumentOutOfRangeException(nameof(format))
                };
            }
            catch (IOException ex)
            {
                throw new ProductFileReadException(
                    "导入文件正在被其他程序占用，请关闭正在使用该文件的 Excel 或 WPS 后重新导入。",
                    ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ProductFileReadException(
                    "没有权限读取导入文件，请检查文件权限或选择其他文件。",
                    ex);
            }

            AddDuplicateErrors(result);
            if (result.TotalRows == 0 && result.Errors.Count == 0)
            {
                result.Errors.Add(new ProductImportError(0, "文件", "文件中没有商品数据。"));
            }

            return result;
        }

        public void Export(
            string path,
            ProductFileFormat format,
            IReadOnlyCollection<ProductQueryDto> products)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentNullException.ThrowIfNull(products);

            var rows = products.Select(product => new ProductCreateDto
            {
                Code = product.Code,
                Name = product.Name,
                Price = product.Price,
                Stock = product.Stock,
                Description = product.Description
            });

            WriteRows(path, format, rows);
        }

        public void WriteTemplate(string path, ProductFileFormat format)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            WriteRows(path, format, Array.Empty<ProductCreateDto>());
        }

        public void WriteErrorReport(
            string path,
            IReadOnlyCollection<ProductImportError> errors)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentNullException.ThrowIfNull(errors);
            EnsureDestinationWritable(path);

            using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteField("行号");
            csv.WriteField("字段");
            csv.WriteField("错误原因");
            csv.NextRecord();

            foreach (var error in errors)
            {
                csv.WriteField(error.RowNumber == 0
                    ? string.Empty
                    : error.RowNumber.ToString(CultureInfo.InvariantCulture));
                csv.WriteField(error.Field);
                csv.WriteField(error.Message);
                csv.NextRecord();
            }
        }

        public static ProductFileFormat GetFormatFromPath(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".csv" => ProductFileFormat.Csv,
                ".xlsx" => ProductFileFormat.Xlsx,
                _ => throw new NotSupportedException("仅支持 CSV 和 XLSX 文件。")
            };
        }

        private static ProductImportReadResult ReadCsv(string path)
        {
            var result = new ProductImportReadResult();
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = context => result.Errors.Add(
                    new ProductImportError(
                        context.Context?.Parser?.Row ?? 0,
                        "文件",
                        "CSV 行格式不正确。"))
            };

            using var reader = new StreamReader(path, Encoding.UTF8, true);
            using var csv = new CsvReader(reader, configuration);
            if (!csv.Read() || !csv.ReadHeader())
            {
                return result;
            }

            var actualHeaders = csv.HeaderRecord ?? Array.Empty<string>();
            AddMissingHeaderErrors(actualHeaders, result);
            if (result.Errors.Count > 0)
            {
                return result;
            }

            while (csv.Read())
            {
                var values = Headers.Select(header => csv.GetField(header) ?? string.Empty).ToArray();
                if (values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                result.TotalRows++;
                ParseRow(csv.Parser.Row, values, result);
            }

            return result;
        }

        private static ProductImportReadResult ReadXlsx(string path)
        {
            var result = new ProductImportReadResult();
            using var workbook = new XLWorkbook(path);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                return result;
            }

            var lastColumn = Math.Max(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, Headers.Length);
            var actualHeaders = Enumerable.Range(1, lastColumn)
                .Select(column => worksheet.Cell(1, column).GetString().Trim())
                .ToArray();
            AddMissingHeaderErrors(actualHeaders, result);
            if (result.Errors.Count > 0)
            {
                return result;
            }

            var headerColumns = Headers.ToDictionary(
                header => header,
                header => Array.FindIndex(actualHeaders, value => value == header) + 1);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var row = 2; row <= lastRow; row++)
            {
                var values = Headers
                    .Select(header => worksheet.Cell(row, headerColumns[header]).GetFormattedString())
                    .ToArray();
                if (values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                result.TotalRows++;
                ParseRow(row, values, result);
            }

            return result;
        }

        private static void ParseRow(
            int rowNumber,
            IReadOnlyList<string> values,
            ProductImportReadResult result)
        {
            var code = values[0].Trim();
            var name = values[1].Trim();
            var description = values[4].Trim();
            AddValidationError(rowNumber, "商品编码", ProductValidationRules.GetCodeError(code), result);
            AddValidationError(rowNumber, "商品名称", ProductValidationRules.GetNameError(name), result);
            AddValidationError(rowNumber, "描述", ProductValidationRules.GetDescriptionError(description), result);

            var priceValid = decimal.TryParse(
                values[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var price);
            if (!priceValid)
            {
                result.Errors.Add(new ProductImportError(rowNumber, "价格", "价格必须是有效数字。"));
            }
            else
            {
                AddValidationError(rowNumber, "价格", ProductValidationRules.GetPriceError(price), result);
            }

            var stockValid = int.TryParse(
                values[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock);
            if (!stockValid)
            {
                result.Errors.Add(new ProductImportError(rowNumber, "库存", "库存必须是整数。"));
            }
            else
            {
                AddValidationError(rowNumber, "库存", ProductValidationRules.GetStockError(stock), result);
            }

            if (result.Errors.Any(error => error.RowNumber == rowNumber))
            {
                return;
            }

            result.Records.Add(new ProductImportRecord(rowNumber, new ProductCreateDto
            {
                Code = code,
                Name = name,
                Price = price,
                Stock = stock,
                Description = description
            }));
        }

        private static void AddDuplicateErrors(ProductImportReadResult result)
        {
            foreach (var group in result.Records.GroupBy(
                         record => record.Product.Code,
                         StringComparer.OrdinalIgnoreCase))
            {
                if (group.Count() < 2)
                {
                    continue;
                }

                foreach (var record in group)
                {
                    result.Errors.Add(new ProductImportError(
                        record.RowNumber,
                        "商品编码",
                        $"商品编码“{record.Product.Code}”在导入文件中重复。"));
                }
            }
        }

        private static void AddMissingHeaderErrors(
            IReadOnlyCollection<string> actualHeaders,
            ProductImportReadResult result)
        {
            foreach (var header in Headers.Except(actualHeaders, StringComparer.Ordinal))
            {
                result.Errors.Add(new ProductImportError(1, header, $"缺少必需列“{header}”。"));
            }
        }

        private static void AddValidationError(
            int rowNumber,
            string field,
            string? message,
            ProductImportReadResult result)
        {
            if (message != null)
            {
                result.Errors.Add(new ProductImportError(rowNumber, field, message));
            }
        }

        private static void WriteRows(
            string path,
            ProductFileFormat format,
            IEnumerable<ProductCreateDto> rows)
        {
            EnsureDestinationWritable(path);

            switch (format)
            {
                case ProductFileFormat.Csv:
                    WriteCsv(path, rows);
                    break;
                case ProductFileFormat.Xlsx:
                    WriteXlsx(path, rows);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        private static void EnsureDestinationWritable(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                // Excel/WPS 常以独占方式打开工作簿；生成前检测可避免覆盖到一半才失败。
                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException ex)
            {
                throw new ProductFileWriteException(
                    "目标文件正在被其他程序占用，请关闭正在使用该文件的 Excel 或 WPS 后重试。",
                    ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ProductFileWriteException(
                    "没有权限覆盖目标文件，请选择其他保存位置或检查文件权限。",
                    ex);
            }
        }

        private static void WriteCsv(string path, IEnumerable<ProductCreateDto> rows)
        {
            using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            foreach (var header in Headers)
            {
                csv.WriteField(header);
            }
            csv.NextRecord();

            foreach (var row in rows)
            {
                csv.WriteField(row.Code);
                csv.WriteField(row.Name);
                csv.WriteField(row.Price);
                csv.WriteField(row.Stock);
                csv.WriteField(row.Description);
                csv.NextRecord();
            }
        }

        private static void WriteXlsx(string path, IEnumerable<ProductCreateDto> rows)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("商品");
            worksheet.TabColor = XLColor.FromHtml("#409EFF");

            for (var column = 0; column < Headers.Length; column++)
            {
                worksheet.Cell(1, column + 1).Value = Headers[column];
            }

            var headerRange = worksheet.Range(1, 1, 1, Headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#409EFF");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#2E7DCC");
            worksheet.Row(1).Height = 26;

            var rowNumber = 2;
            foreach (var row in rows)
            {
                worksheet.Cell(rowNumber, 1).Value = row.Code;
                worksheet.Cell(rowNumber, 2).Value = row.Name;
                worksheet.Cell(rowNumber, 3).Value = row.Price;
                worksheet.Cell(rowNumber, 4).Value = row.Stock;
                worksheet.Cell(rowNumber, 5).Value = row.Description;
                worksheet.Row(rowNumber).Height = 22;

                if (rowNumber % 2 == 0)
                {
                    worksheet.Range(rowNumber, 1, rowNumber, Headers.Length)
                        .Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F7FA");
                }

                rowNumber++;
            }

            var lastRow = Math.Max(1, rowNumber - 1);
            var dataRange = worksheet.Range(1, 1, lastRow, Headers.Length);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#DCDFE6");
            dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E4E7ED");
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // 固定最小宽度，避免空模板或短数据让中文表头被压缩截断。
            worksheet.Column(1).Width = 16;
            worksheet.Column(2).Width = 24;
            worksheet.Column(3).Width = 14;
            worksheet.Column(4).Width = 12;
            worksheet.Column(5).Width = 38;
            worksheet.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            worksheet.Column(3).Style.NumberFormat.Format = "0.00";
            worksheet.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(4).Style.NumberFormat.Format = "0";
            worksheet.Column(5).Style.Alignment.WrapText = true;

            dataRange.SetAutoFilter();
            worksheet.SheetView.FreezeRows(1);
            worksheet.SheetView.FreezeColumns(1);
            worksheet.PageSetup.CenterHorizontally = true;
            worksheet.PageSetup.FitToPages(1, 0);
            workbook.SaveAs(path);
        }
    }
}
