using ProductManagerApp.DTO;
using ProductManagerApp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagerApp.BLL.Mappers
{
    /// <summary>
    ///  DTO->Entity的翻译层（Mapper），也可以放在BLL.Services里，但为了职责单一，放在独立的Mapper类里
    /// </summary>
    public static class ProductMapper
    {
        public static Product ToEntity(ProductCreateDto dto)
        {
            return new Product
            {
                Code = dto.Code,
                Name = dto.Name,
                Price = dto.Price,
                Stock = dto.Stock,
                Description = dto.Description
            };
        }
        public static Product ToEntity(ProductUpdateDto dto)
        {
            return new Product
            {
                Id = dto.Id,
                Code = dto.Code,
                Name = dto.Name,
                Price = dto.Price,
                Stock = dto.Stock,
                Description = dto.Description
            };
        }
        public static ProductQueryDto ToQueryDto(Product product)
        {
            return new ProductQueryDto
            {
                Id = product.Id,
                Code = product.Code,
                Name = product.Name,
                Price = product.Price,
                Stock = product.Stock,
                Description = product.Description
            };
        }
    }
}

