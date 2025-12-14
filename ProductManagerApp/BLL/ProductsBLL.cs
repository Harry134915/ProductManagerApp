using ProductManagerApp.BLL.Exceptions;
using ProductManagerApp.DAL;
using ProductManagerApp.Entity;
using ProductManagerApp.DTO;
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
        public void AddProduct(ProductCreateDto dto)
        {
            //1.防御性编程(DTO本身不可为空)
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            //2.DTO -> Entity (此处是"翻译层")
            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Stock = dto.Stock,
                Description = dto.Description
            };

            //3.业务校验(只认Entity)
            ValidateProduct(product);

            //4.通过校验，允许调dal
            _productDAL.AddProduct(product);
        }

        // ============================================================
        // 更新
        // ============================================================
        public void UpdateProduct(ProductUpdateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.Id <= 0)
            {
                throw new ProductValidationException("id不合法！");
            }

            var product = new Product
            {
                Id = dto.Id,
                Name = dto.Name,
                Price = dto.Price,
                Stock = dto.Stock,
                Description = dto.Description
            };

            ValidateProduct(product);

            _productDAL.UpdateProduct(product);

            //nameof 是 C# 的一个编译期关键字，将“代码里的名字”，安全地变成“字符串”
            //你可以调用我，但前提是 product 不能为空
            //如果你违反契约，我会立刻报错
        }

        // ============================================================
        // 更新价格
        // ============================================================
        public void UpdateProductPrice(int productId, decimal newPrice)
        {
            if (productId <= 0)
                throw new ProductValidationException("商品id不合法！");

            if (newPrice <= 0)
                throw new ProductValidationException("价格必须大于0！");

            _productDAL.UpdateProductPrice(productId, newPrice);
        }

        // ============================================================
        // 删除
        // ============================================================
        public void DeleteProduct(int productId)
        {
            if (productId <= 0)
                throw new ProductValidationException("商品id不合法！");

            _productDAL.DeleteProduct(productId);
        }

        // ============================================================
        // 私有方法：商品业务校验
        // ============================================================
        //ValidateAndSetError 是 UI 校验/格式化
        //ValidateProduct 是 核心业务校验
        private void ValidateProduct(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                throw new ProductValidationException("商品名称不能为空！");
            }

            if (product.Price <= 0)
            {
                throw new ProductValidationException("价格必须大于0！");
            }

            if (product.Stock < 0)
            {
                throw new ProductValidationException("库存不能为负数！");
            }

            if (product.Description != null &&
                string.IsNullOrWhiteSpace(product.Description))
            {
                throw new ProductValidationException("描述不能只有空白字符！");
            }
        }

    }
}