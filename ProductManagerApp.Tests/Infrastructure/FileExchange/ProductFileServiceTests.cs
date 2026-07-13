using ClosedXML.Excel;
using ProductManagerApp.DTO;
using ProductManagerApp.Infrastructure.FileExchange;
using System.Text;

namespace ProductManagerApp.Tests.Infrastructure.FileExchange;

/// <summary>
/// 验证商品 CSV/XLSX 的格式约定、预检错误和可往返导入的导出内容。
/// </summary>
public class ProductFileServiceTests
{
    private readonly ProductFileService _service = new();

    [Fact]
    public void ReadImport_CsvWithQuotedComma_ReturnsValidRecord()
    {
        var path = CreateTempPath(".csv");
        try
        {
            File.WriteAllText(path,
                "商品编码,商品名称,价格,库存,描述\r\nP001,Phone,1999.50,10,\"Good, useful\"\r\n",
                new UTF8Encoding(true));

            var result = _service.ReadImport(path, ProductFileFormat.Csv);

            Assert.True(result.IsValid);
            var record = Assert.Single(result.Records);
            Assert.Equal(2, record.RowNumber);
            Assert.Equal("Good, useful", record.Product.Description);
            Assert.Equal(1999.50m, record.Product.Price);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadImport_CsvWithMissingHeader_ReturnsHeaderError()
    {
        var path = CreateTempPath(".csv");
        try
        {
            File.WriteAllText(path, "商品编码,商品名称\r\nP001,Phone\r\n", Encoding.UTF8);

            var result = _service.ReadImport(path, ProductFileFormat.Csv);

            Assert.Contains(result.Errors, error => error.Field == "价格");
            Assert.Empty(result.Records);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadImport_CsvWithInvalidValues_ReturnsFieldErrors()
    {
        var path = CreateTempPath(".csv");
        try
        {
            File.WriteAllText(path,
                "商品编码,商品名称,价格,库存,描述\r\n中文,Phone,abc,-1,\r\n",
                Encoding.UTF8);

            var result = _service.ReadImport(path, ProductFileFormat.Csv);

            Assert.Contains(result.Errors, error => error.Field == "商品编码");
            Assert.Contains(result.Errors, error => error.Field == "价格");
            Assert.Contains(result.Errors, error => error.Field == "库存");
            Assert.Contains(result.Errors, error => error.Field == "描述");
            Assert.Empty(result.Records);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadImport_XlsxIgnoresBlankRowsAndFindsDuplicateCodes()
    {
        var path = CreateTempPath(".xlsx");
        try
        {
            _service.WriteTemplate(path, ProductFileFormat.Xlsx);
            using (var workbook = new XLWorkbook(path))
            {
                var sheet = workbook.Worksheet(1);
                WriteXlsxRow(sheet, 2, "P001", "Phone", 100m, 1, "First");
                WriteXlsxRow(sheet, 4, "p001", "Phone 2", 200m, 2, "Second");
                workbook.Save();
            }

            var result = _service.ReadImport(path, ProductFileFormat.Xlsx);

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(2, result.Errors.Count(error => error.Field == "商品编码"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(ProductFileFormat.Csv, ".csv")]
    [InlineData(ProductFileFormat.Xlsx, ".xlsx")]
    public void Export_CanBeReadBackWithoutDatabaseId(ProductFileFormat format, string extension)
    {
        var path = CreateTempPath(extension);
        try
        {
            _service.Export(path, format, new[]
            {
                new ProductQueryDto
                {
                    Id = 99,
                    Code = "P001",
                    Name = "Phone",
                    Price = 1999m,
                    Stock = 10,
                    Description = "Flagship"
                }
            });

            var result = _service.ReadImport(path, format);

            Assert.True(result.IsValid);
            Assert.Equal("P001", Assert.Single(result.Records).Product.Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void WriteErrorReport_UsesUtf8BomAndStructuredColumns()
    {
        var path = CreateTempPath(".csv");
        try
        {
            _service.WriteErrorReport(path, new[]
            {
                new ProductImportError(2, "商品编码", "编码重复")
            });

            var bytes = File.ReadAllBytes(path);
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3));
            Assert.Contains("编码重复", File.ReadAllText(path, Encoding.UTF8));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void WriteTemplate_XlsxUsesReadableColumnWidthsAndHeaderStyle()
    {
        var path = CreateTempPath(".xlsx");
        try
        {
            _service.WriteTemplate(path, ProductFileFormat.Xlsx);

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet(1);
            Assert.True(sheet.Column(1).Width >= 16);
            Assert.True(sheet.Column(2).Width >= 24);
            Assert.True(sheet.Column(5).Width >= 38);
            Assert.True(sheet.Cell(1, 1).Style.Font.Bold);
            Assert.Equal(XLColor.White, sheet.Cell(1, 1).Style.Font.FontColor);
            Assert.Equal("商品编码", sheet.Cell(1, 1).GetString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void WriteTemplate_WhenExistingFileIsLocked_ReturnsFriendlyWriteError()
    {
        var path = CreateTempPath(".xlsx");
        try
        {
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            using var lockStream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            var exception = Assert.Throws<ProductFileWriteException>(
                () => _service.WriteTemplate(path, ProductFileFormat.Xlsx));

            Assert.Contains("Excel 或 WPS", exception.Message);
            lockStream.Dispose();
            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadImport_WhenXlsxIsLocked_ReturnsFriendlyReadError()
    {
        var path = CreateTempPath(".xlsx");
        try
        {
            _service.WriteTemplate(path, ProductFileFormat.Xlsx);
            using var lockStream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            var exception = Assert.Throws<ProductFileReadException>(
                () => _service.ReadImport(path, ProductFileFormat.Xlsx));

            Assert.Contains("关闭正在使用该文件的 Excel 或 WPS", exception.Message);
            lockStream.Dispose();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTempPath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), $"ProductFile-{Guid.NewGuid():N}{extension}");
    }

    private static void WriteXlsxRow(
        IXLWorksheet sheet,
        int row,
        string code,
        string name,
        decimal price,
        int stock,
        string description)
    {
        sheet.Cell(row, 1).Value = code;
        sheet.Cell(row, 2).Value = name;
        sheet.Cell(row, 3).Value = price;
        sheet.Cell(row, 4).Value = stock;
        sheet.Cell(row, 5).Value = description;
    }
}
