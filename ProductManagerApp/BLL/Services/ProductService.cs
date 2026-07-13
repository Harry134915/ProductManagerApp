using ProductManagerApp.Infrastructure.Exceptions;
using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.BLL.Mappers;
using ProductManagerApp.BLL.Validators;
using ProductManagerApp.DAL;
using ProductManagerApp.DTO;

namespace ProductManagerApp.BLL.Services
{
    /// <summary>
    /// 执行商品业务校验、模型映射和 Repository 调用结果检查。
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly ProductValidator _validator;

        public ProductService(IProductRepository repo, ProductValidator validator)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public List<ProductQueryDto> GetAllProducts()
        {
            var products = _repo.GetAllProducts();

            return products
                .Select(ProductMapper.ToQueryDto)
                .ToList();
        }

        public void AddProduct(ProductCreateDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var entity = ProductMapper.ToEntity(dto);

            _validator.Validate(entity);

            if (_repo.GetProductByCode(entity.Code) != null)
            {
                throw new ProductValidationException($"商品编码“{entity.Code}”已存在，请使用其他编码。");
            }

            var affected = _repo.AddProduct(entity);
            if (affected == 0)
            {
                throw new ProductValidationException(
                    "新增失败，未写入商品数据，请稍后重试。");
            }
        }

        public void UpdateProduct(ProductUpdateDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            _validator.ValidateId(dto.Id);

            var currentProduct = _repo.GetProductById(dto.Id);
            if (currentProduct == null)
                throw new ProductValidationException("更新失败，未找到对应的商品。");

            _validator.ValidateCodeUnchanged(currentProduct.Code, dto.Code);

            var entity = ProductMapper.ToEntity(dto);

            _validator.Validate(entity);

            var affected = _repo.UpdateProduct(entity);
            if (affected == 0)
            {
                throw new ProductValidationException(
                    "更新失败，商品可能已被删除，请刷新列表后重试。");
            }
        }

        public void UpdateProductPrice(int productId, decimal newPrice)
        {
            _validator.ValidateId(productId);

            _validator.ValidatePrice(newPrice);

            var affected = _repo.UpdateProductPrice(productId, newPrice);
            if (affected == 0)
                throw new ProductValidationException("更新价格失败，未找到对应的商品。");
        }

        public void DeleteProduct(int productId)
        {
            _validator.ValidateId(productId);

            var affected = _repo.DeleteProduct(productId);
            if (affected == 0)
            {
                throw new ProductValidationException(
                    "删除失败，商品可能已被删除，请刷新列表后重试。");
            }
        }
    }
}
