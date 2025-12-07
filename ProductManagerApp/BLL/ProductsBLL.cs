//using ProductManagerApp.DAL;
//using ProductManagerApp.Models;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ProductManagerApp.BLL
//{
//    internal class ProductsBLL : IProductsBLL
//    {
//        private readonly IProductsDAL _productDAL;

//        public ProductsBLL()
//        {
//            _productDAL = new ProductsSqliteDAL();
//        }

//        //BLL：将DataTable➡List<Product>

//        public List<Product> GetAllProducts()
//        {
//            DataTable table = _productDAL.QueryProducts();

//            List<Product> products = new List<Product>();

//            foreach (DataRow row in table.Rows)
//            {
//                products.Add(MapToProduct(row));
//            }
//            return products;
//        }

//        //public void AddProduct(Product product)
//        //{
//        //    _productDAL.AddProduct(product);
//        //}

//        public void UpdateProductPrice(int productId, double newPrice)
//        {
//            //BLL 决定"更新什么"
//            _productDAL.UpdateProductPrice(productId, newPrice); 

//            //TODO:如果我想要有一个更新指定字段的方法.

//        }

//        public void UpdateProduct(Product product)
//        {
//            _productDAL.UpdateProduct(product);
//        }

//        public void DeleteProduct(int productId)
//        {
//            _productDAL.QueryProductById(productId);
//        }

//        private Product MapToProduct(DataRow row)
//        {
//            return new Product
//            {
//                Id = Convert.ToInt32(row["id"]),
//                Name = row["Name"].ToString()!,
//                Price = Convert.ToDouble(row["Price"]),
//                Description = row["Description"]?.ToString(),
//            };
//        }
//        private DataRow MapToRow(Product product, DataTable schema)
//        {
//            DataRow row = schema.NewRow();
//            row["id"] = product.Id;
//            row["name"] = product.Name;
//            row["price"] = product.Price;
//            row["stock"] = product.Stock;
//            row["description"] = product.Description;
//            return row;
//        }

//        public void AddProduct(Product product)
//        {
//            DataTable schema = _productDAL.QueryProducts();
//            DataRow row = MapToRow(product, schema);
//            _productDAL.AddProduct(row);
//        }


//    }
//}

using ProductManagerApp.DAL;
using ProductManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace ProductManagerApp.BLL
{
    internal class ProductsBLL : IProductsBLL
    {
        private readonly IProductsDAL _productDAL;

        public ProductsBLL()
        {
            _productDAL = new ProductsSqliteDAL();
        }

        public List<Product> GetAllProducts()
        {
            var dt = _productDAL.QueryProducts();
            var list = new List<Product>();
            if (dt == null) return list;

            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapToProduct(row));
            }
            return list;
        }

        public void AddProduct(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            DataTable schema = GetSchemaTable();
            DataRow row = MapToRow(product, schema);
            _productDAL.AddProduct(row);
        }

        public void UpdateProduct(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            DataTable schema = GetSchemaTable();
            DataRow row = MapToRow(product, schema); // 包含 id 字段
            _productDAL.UpdateProduct(row);
        }

        public void UpdateProductPrice(int productId, double newPrice)
        {
            _productDAL.UpdateProductPrice(productId, newPrice);
        }

        public void DeleteProduct(int productId)
        {
            _productDAL.DeleteProduct(productId);
        }

        #region 映射辅助方法

        private Product MapToProduct(DataRow row)
        {
            if (row == null) return null!; // 你可以根据项目的 null 策略改成抛异常或返回 null
            return new Product
            {
                Id = row.Table.Columns.Contains("id") && row["id"] != DBNull.Value ? Convert.ToInt32(row["id"]) : 0,
                Name = row.Table.Columns.Contains("name") && row["name"] != DBNull.Value ? row["name"].ToString()! : string.Empty,
                Price = row.Table.Columns.Contains("price") && row["price"] != DBNull.Value ? Convert.ToDouble(row["price"]) : 0.0,
                Stock = row.Table.Columns.Contains("stock") && row["stock"] != DBNull.Value ? Convert.ToInt32(row["stock"]) : 0,
                Description = row.Table.Columns.Contains("description") && row["description"] != DBNull.Value ? row["description"].ToString()! : string.Empty
            };
        }

        private DataRow MapToRow(Product product, DataTable schemaTable)
        {
            if (schemaTable == null) throw new ArgumentNullException(nameof(schemaTable));
            DataRow row = schemaTable.NewRow();

            // 根据列名逐一填充（防空与类型转换）
            if (schemaTable.Columns.Contains("id")) row["id"] = product.Id;
            if (schemaTable.Columns.Contains("name")) row["name"] = (object?)product.Name ?? DBNull.Value;
            if (schemaTable.Columns.Contains("price")) row["price"] = product.Price;
            if (schemaTable.Columns.Contains("stock")) row["stock"] = product.Stock;
            if (schemaTable.Columns.Contains("description")) row["description"] = (object?)product.Description ?? DBNull.Value;

            return row;
        }

        /// <summary>
        /// 获取一个可用来 NewRow() 的 DataTable（使用 QueryProducts() 得到的结构）
        /// 如果表为空，仍然可用，因为 NewRow() 只使用 schema。
        /// </summary>
        private DataTable GetSchemaTable()
        {
            DataTable dt = _productDAL.QueryProducts();
            if (dt == null)
            {
                // 如果 DAL 返回 null（不应该），创建一个最小 schema 避免空引用
                dt = new DataTable();
                dt.Columns.Add("id", typeof(int));
                dt.Columns.Add("name", typeof(string));
                dt.Columns.Add("price", typeof(double));
                dt.Columns.Add("stock", typeof(int));
                dt.Columns.Add("description", typeof(string));
            }
            return dt;
        }

        #endregion
    }
}
