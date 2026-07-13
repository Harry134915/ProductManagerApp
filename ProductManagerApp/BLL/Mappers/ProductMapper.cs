using ProductManagerApp.DTO;
using ProductManagerApp.Entity;

namespace ProductManagerApp.BLL.Mappers
{
    /// <summary>
    /// 在商品 DTO 与持久化 Entity 之间进行无业务逻辑的字段映射。
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

