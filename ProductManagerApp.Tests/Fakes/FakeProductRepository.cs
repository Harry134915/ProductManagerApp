using ProductManagerApp.DAL;
using ProductManagerApp.Entity;

namespace ProductManagerApp.Tests.Fakes;

/// <summary>
/// 以内存集合模拟 Repository，并记录调用参数和可配置的 affected rows。
/// </summary>
internal sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Products { get; } = new();

    public int AddProductCallCount { get; private set; }
    public int AddProductsCallCount { get; private set; }
    public int UpdateProductCallCount { get; private set; }
    public int UpdateProductPriceCallCount { get; private set; }
    public int DeleteProductCallCount { get; private set; }

    public Product? LastAddedProduct { get; private set; }
    public Product? LastUpdatedProduct { get; private set; }
    public int? LastDeletedProductId { get; private set; }

    public int AddProductResult { get; set; } = 1;
    public int? AddProductsResult { get; set; }
    public int UpdateProductResult { get; set; } = 1;
    public int UpdateProductPriceResult { get; set; } = 1;
    public int DeleteProductResult { get; set; } = 1;
    public Exception? GetAllProductsException { get; set; }

    public List<Product> GetAllProducts()
    {
        if (GetAllProductsException != null)
        {
            throw GetAllProductsException;
        }

        return Products.ToList();
    }

    public Product? GetProductById(int id)
    {
        return Products.FirstOrDefault(product => product.Id == id);
    }

    public Product? GetProductByCode(string code)
    {
        return Products.FirstOrDefault(product =>
            string.Equals(product.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    public int AddProduct(Product product)
    {
        AddProductCallCount++;
        LastAddedProduct = product;
        if (AddProductResult > 0)
        {
            Products.Add(product);
        }

        return AddProductResult;
    }

    public int AddProducts(IReadOnlyCollection<Product> products)
    {
        AddProductsCallCount++;
        var affected = AddProductsResult ?? products.Count;
        if (affected == products.Count)
        {
            Products.AddRange(products);
        }

        return affected;
    }

    public int UpdateProduct(Product product)
    {
        UpdateProductCallCount++;
        LastUpdatedProduct = product;
        return UpdateProductResult;
    }

    public int UpdateProductPrice(int productId, decimal newPrice)
    {
        UpdateProductPriceCallCount++;
        return UpdateProductPriceResult;
    }

    public int DeleteProduct(int productId)
    {
        DeleteProductCallCount++;
        LastDeletedProductId = productId;
        return DeleteProductResult;
    }
}
