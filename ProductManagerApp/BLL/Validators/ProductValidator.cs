using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProductManagerApp.Entity;
using ProductManagerApp.BLL.Exceptions;


namespace ProductManagerApp.BLL.Validators
{
    public class ProductValidator
    {
        public void Validate(Product product)
        {
            if (product == null)
                throw new ProductValidationException("商品不可为空!");

            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ProductValidationException("商品名称不能为空！");

            if (string.IsNullOrWhiteSpace(product.Code))
                throw new ProductValidationException("商品编码不能为空！");

            if (product.Price <= 0)
                throw new ProductValidationException("价格必须大于0！");

            if (product.Stock < 0)
                throw new ProductValidationException("库存不能为负数！");

            if (product.Description != null &&
                string.IsNullOrWhiteSpace(product.Description))
            {
                throw new ProductValidationException("描述不能只有空白字符！");
            }
        }
        public void ValidateId(int id)
        {
            if (id <= 0)
                throw new ProductValidationException("商品id不合法！");
        }
        public void ValidatePrice(decimal price)
        {
            if (price <= 0)
                throw new ProductValidationException("价格必须大于0！");
        }
    }
}
