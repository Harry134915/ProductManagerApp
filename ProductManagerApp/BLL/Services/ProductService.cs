using ProductManagerApp.BLL.Exceptions;
using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.BLL.Mappers;
using ProductManagerApp.BLL.Validators;
using ProductManagerApp.DAL;
using ProductManagerApp.DTO;

namespace ProductManagerApp.BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly ProductValidator _validator;

        // 构造函数注入依赖，不再强绑定SqlliteDAL
        public ProductService(IProductRepository repo, ProductValidator validator)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        // ============================================================
        // 查询全部（这里返回Dto，不返回Entity）
        // ============================================================
        public List<ProductQueryDto> GetAllProducts()
        {
            var products = _repo.GetAllProducts();

            return products
                .Select(ProductMapper.ToQueryDto)
                .ToList();
        }

        // ============================================================
        // 新增
        // ============================================================
        public void AddProduct(ProductCreateDto dto)
        {
            //1.DTO本身不可为空
            //if (dto == null)
            //    throw new ArgumentNullException(nameof(dto));

            ArgumentNullException.ThrowIfNull(dto);

            //2.格式校验(只认DTO)
            var entity = ProductMapper.ToEntity(dto);

            //3.业务校验(只认Entity)
            _validator.Validate(entity);

            //4.通过校验，允许调DAL进行数据库操作
            _repo.AddProduct(entity);
        }

        // ============================================================
        // 更新
        // ============================================================
        public void UpdateProduct(ProductUpdateDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            _validator.ValidateId(dto.Id);

            var entity = ProductMapper.ToEntity(dto);

            _validator.Validate(entity);

            _repo.UpdateProduct(entity);
        }

        // ============================================================
        // 更新价格
        // ============================================================
        public void UpdateProductPrice(int productId, decimal newPrice)
        {
            _validator.ValidateId(productId);

            _validator.ValidatePrice(newPrice);

            var affected = _repo.UpdateProductPrice(productId, newPrice);
            if (affected == 0)
                throw new ProductValidationException("更新价格失败，未找到对应的商品。");
        }

        // ============================================================
        // 删除
        // ============================================================
        public void DeleteProduct(int productId)
        {
            _validator.ValidateId(productId);

            _repo.DeleteProduct(productId);
        }
    }
}