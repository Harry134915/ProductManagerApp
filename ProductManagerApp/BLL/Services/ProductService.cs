using ProductManagerApp.BLL.Exceptions;
using ProductManagerApp.DAL;
using ProductManagerApp.Entity;
using ProductManagerApp.DTO;
using System;
using System.Collections.Generic;
using ProductManagerApp.BLL.Interfaces;
using ProductManagerApp.BLL.Validators;
using ProductManagerApp.BLL.Mappers;

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
            //1.防御性编程(DTO本身不可为空)
            //if (dto == null)
            //    throw new ArgumentNullException(nameof(dto));

            ArgumentNullException.ThrowIfNull(dto);

            //2.格式校验(只认DTO)
            var entity = ProductMapper.ToEntity(dto);

            //3.业务校验(只认Entity)
            _validator.Validate(entity);

            //4.通过校验，允许调dal
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

            //nameof 是 C# 的一个编译期关键字，将“代码里的名字”，安全地变成“字符串”
            //你可以调用我，但前提是 product 不能为空
            //如果你违反契约，我会立刻报错
        }

        // ============================================================
        // 更新价格
        // ============================================================
        public void UpdateProductPrice(int productId, decimal newPrice)
        {
            _validator.ValidateId(productId);

            _validator.ValidatePrice(newPrice);

            _repo.UpdateProductPrice(productId, newPrice);
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