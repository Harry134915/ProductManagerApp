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

namespace ProductManagerApp.BLL
{
    internal class ProductsBLL : IProductsBLL
    {
        private readonly IProductsDAL _productDAL;

        public ProductsBLL()
        {
            _productDAL = new ProductsSqliteDAL();
        }

        // ============================================================
        // 查询全部
        // ============================================================
        public List<Product> GetAllProducts()
        {
            return _productDAL.GetAllProducts();
        }

        // ============================================================
        // 新增
        // ============================================================
        public void AddProduct(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            _productDAL.AddProduct(product);
        }

        // ============================================================
        // 更新
        // ============================================================
        public void UpdateProduct(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            _productDAL.UpdateProduct(product);
        }

        // ============================================================
        // 更新价格
        // ============================================================
        public void UpdateProductPrice(int productId, double newPrice)
        {
            _productDAL.UpdateProductPrice(productId, newPrice);
        }

        // ============================================================
        // 删除
        // ============================================================
        public void DeleteProduct(int productId)
        {
            _productDAL.DeleteProduct(productId);
        }
    }
}