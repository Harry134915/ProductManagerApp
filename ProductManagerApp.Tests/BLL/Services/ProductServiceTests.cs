using ProductManagerApp.BLL.Services;
using ProductManagerApp.BLL.Validators;
using ProductManagerApp.DTO;
using ProductManagerApp.Entity;
using ProductManagerApp.Infrastructure.Exceptions;
using ProductManagerApp.Tests.Fakes;

namespace ProductManagerApp.Tests.BLL.Services;

public class ProductServiceTests
{
    [Fact]
    public void GetAllProducts_ReturnsMappedDtos()
    {
        var repo = new FakeProductRepository();
        repo.Products.Add(new Product
        {
            Id = 1,
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        });
        var service = CreateService(repo);

        var products = service.GetAllProducts();

        var product = Assert.Single(products);
        Assert.Equal(1, product.Id);
        Assert.Equal("P001", product.Code);
        Assert.Equal("Phone", product.Name);
        Assert.Equal(1999m, product.Price);
        Assert.Equal(10, product.Stock);
        Assert.Equal("Flagship phone", product.Description);
    }

    [Fact]
    public void AddProduct_WithValidDto_CallsRepository()
    {
        var repo = new FakeProductRepository();
        var service = CreateService(repo);

        service.AddProduct(CreateValidCreateDto());

        Assert.Equal(1, repo.AddProductCallCount);
        Assert.NotNull(repo.LastAddedProduct);
        Assert.Equal("P001", repo.LastAddedProduct.Code);
    }

    [Fact]
    public void AddProduct_WithInvalidDto_ThrowsValidationExceptionAndDoesNotCallRepository()
    {
        var repo = new FakeProductRepository();
        var service = CreateService(repo);
        var dto = CreateValidCreateDto();
        dto.Price = 0;

        Assert.Throws<ProductValidationException>(() => service.AddProduct(dto));
        Assert.Equal(0, repo.AddProductCallCount);
    }

    [Fact]
    public void AddProduct_WhenNoRowsAffected_ThrowsValidationException()
    {
        var repo = new FakeProductRepository
        {
            AddProductResult = 0
        };
        var service = CreateService(repo);

        var exception = Assert.Throws<ProductValidationException>(
            () => service.AddProduct(CreateValidCreateDto()));

        Assert.Equal("新增失败，未写入商品数据，请稍后重试。", exception.Message);
        Assert.Equal(1, repo.AddProductCallCount);
        Assert.Empty(repo.Products);
    }

    [Fact]
    public void AddProduct_WithDuplicateCode_ThrowsValidationExceptionAndDoesNotAdd()
    {
        var repo = new FakeProductRepository();
        repo.Products.Add(CreateExistingProduct());
        var service = CreateService(repo);
        var dto = CreateValidCreateDto();
        dto.Code = "p001";

        var exception = Assert.Throws<ProductValidationException>(
            () => service.AddProduct(dto));

        Assert.Equal("商品编码“p001”已存在，请使用其他编码。", exception.Message);
        Assert.Equal(0, repo.AddProductCallCount);
        Assert.Single(repo.Products);
    }

    [Fact]
    public void UpdateProduct_WhenProductDoesNotExist_ThrowsValidationException()
    {
        var repo = new FakeProductRepository();
        var service = CreateService(repo);

        Assert.Throws<ProductValidationException>(() => service.UpdateProduct(CreateValidUpdateDto()));
        Assert.Equal(0, repo.UpdateProductCallCount);
    }

    [Fact]
    public void UpdateProduct_WhenCodeChanged_ThrowsValidationException()
    {
        var repo = new FakeProductRepository();
        repo.Products.Add(CreateExistingProduct());
        var service = CreateService(repo);
        var dto = CreateValidUpdateDto();
        dto.Code = "P002";

        Assert.Throws<ProductValidationException>(() => service.UpdateProduct(dto));
        Assert.Equal(0, repo.UpdateProductCallCount);
    }

    [Fact]
    public void UpdateProduct_WithValidDto_CallsRepository()
    {
        var repo = new FakeProductRepository();
        repo.Products.Add(CreateExistingProduct());
        var service = CreateService(repo);
        var dto = CreateValidUpdateDto();

        service.UpdateProduct(dto);

        Assert.Equal(1, repo.UpdateProductCallCount);
        Assert.NotNull(repo.LastUpdatedProduct);
        Assert.Equal(dto.Id, repo.LastUpdatedProduct.Id);
        Assert.Equal(dto.Name, repo.LastUpdatedProduct.Name);
    }

    [Fact]
    public void UpdateProduct_WhenNoRowsAffected_ThrowsValidationException()
    {
        var repo = new FakeProductRepository
        {
            UpdateProductResult = 0
        };
        repo.Products.Add(CreateExistingProduct());
        var service = CreateService(repo);

        var exception = Assert.Throws<ProductValidationException>(
            () => service.UpdateProduct(CreateValidUpdateDto()));

        Assert.Equal(
            "更新失败，商品可能已被删除，请刷新列表后重试。",
            exception.Message);
        Assert.Equal(1, repo.UpdateProductCallCount);
    }

    [Fact]
    public void UpdateProductPrice_WhenNoRowsAffected_ThrowsValidationException()
    {
        var repo = new FakeProductRepository
        {
            UpdateProductPriceResult = 0
        };
        var service = CreateService(repo);

        Assert.Throws<ProductValidationException>(() => service.UpdateProductPrice(1, 99m));
        Assert.Equal(1, repo.UpdateProductPriceCallCount);
    }

    [Fact]
    public void DeleteProduct_WithInvalidId_ThrowsValidationExceptionAndDoesNotCallRepository()
    {
        var repo = new FakeProductRepository();
        var service = CreateService(repo);

        Assert.Throws<ProductValidationException>(() => service.DeleteProduct(0));
        Assert.Equal(0, repo.DeleteProductCallCount);
    }

    [Fact]
    public void DeleteProduct_WithValidId_CallsRepository()
    {
        var repo = new FakeProductRepository();
        var service = CreateService(repo);

        service.DeleteProduct(1);

        Assert.Equal(1, repo.DeleteProductCallCount);
        Assert.Equal(1, repo.LastDeletedProductId);
    }

    [Fact]
    public void DeleteProduct_WhenNoRowsAffected_ThrowsValidationException()
    {
        var repo = new FakeProductRepository
        {
            DeleteProductResult = 0
        };
        var service = CreateService(repo);

        var exception = Assert.Throws<ProductValidationException>(
            () => service.DeleteProduct(1));

        Assert.Equal(
            "删除失败，商品可能已被删除，请刷新列表后重试。",
            exception.Message);
        Assert.Equal(1, repo.DeleteProductCallCount);
        Assert.Equal(1, repo.LastDeletedProductId);
    }

    private static ProductService CreateService(FakeProductRepository repo)
    {
        return new ProductService(repo, new ProductValidator());
    }

    private static ProductCreateDto CreateValidCreateDto()
    {
        return new ProductCreateDto
        {
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        };
    }

    private static ProductUpdateDto CreateValidUpdateDto()
    {
        return new ProductUpdateDto
        {
            Id = 1,
            Code = "P001",
            Name = "Phone Pro",
            Price = 2999m,
            Stock = 8,
            Description = "Updated phone"
        };
    }

    private static Product CreateExistingProduct()
    {
        return new Product
        {
            Id = 1,
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        };
    }
}
