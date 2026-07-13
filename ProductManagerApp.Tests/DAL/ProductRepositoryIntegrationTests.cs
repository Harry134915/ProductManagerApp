using ProductManagerApp.Entity;
using ProductManagerApp.Infrastructure.Exceptions;
using System.Data.SQLite;

namespace ProductManagerApp.Tests.DAL;

/// <summary>
/// 在独立临时 SQLite 数据库上验证 ProductRepository 的真实 SQL 行为。
/// </summary>
[Collection(SqliteIntegrationCollection.Name)]
public class ProductRepositoryIntegrationTests
{
    [Fact]
    public void AddAndQueryProducts_UsesRealSqliteDatabase()
    {
        using var database = new SqliteTestDatabase();
        var repository = database.Repository;

        var affected = repository.AddProduct(CreateProduct());

        Assert.Equal(1, affected);
        var saved = Assert.Single(repository.GetAllProducts());
        Assert.True(saved.Id > 0);
        Assert.Equal("P001", saved.Code);
        Assert.Equal("Phone", saved.Name);
        Assert.Equal(1999m, saved.Price);
        Assert.Equal(10, saved.Stock);
        Assert.Equal("Flagship phone", saved.Description);

        Assert.Equal(saved.Id, repository.GetProductById(saved.Id)?.Id);
        Assert.Equal(saved.Id, repository.GetProductByCode("p001")?.Id);
    }

    [Fact]
    public void UpdateProduct_UpdatesEditableFieldsButPreservesCode()
    {
        using var database = new SqliteTestDatabase();
        var repository = database.Repository;
        repository.AddProduct(CreateProduct());
        var saved = Assert.Single(repository.GetAllProducts());

        var affected = repository.UpdateProduct(new Product
        {
            Id = saved.Id,
            Code = "CHANGED",
            Name = "Updated phone",
            Price = 2099m,
            Stock = 8,
            Description = "Updated description"
        });

        Assert.Equal(1, affected);
        var updated = repository.GetProductById(saved.Id);
        Assert.NotNull(updated);
        Assert.Equal("P001", updated.Code);
        Assert.Equal("Updated phone", updated.Name);
        Assert.Equal(2099m, updated.Price);
        Assert.Equal(8, updated.Stock);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public void UpdateProductPrice_ChangesOnlyPrice()
    {
        using var database = new SqliteTestDatabase();
        var repository = database.Repository;
        repository.AddProduct(CreateProduct());
        var saved = Assert.Single(repository.GetAllProducts());

        var affected = repository.UpdateProductPrice(saved.Id, 2499m);

        Assert.Equal(1, affected);
        var updated = repository.GetProductById(saved.Id);
        Assert.NotNull(updated);
        Assert.Equal(2499m, updated.Price);
        Assert.Equal(saved.Code, updated.Code);
        Assert.Equal(saved.Name, updated.Name);
        Assert.Equal(saved.Stock, updated.Stock);
        Assert.Equal(saved.Description, updated.Description);
    }

    [Fact]
    public void WriteMethods_WhenProductDoesNotExist_ReturnZeroAffectedRows()
    {
        using var database = new SqliteTestDatabase();
        var repository = database.Repository;

        var updateAffected = repository.UpdateProduct(new Product
        {
            Id = 999,
            Code = "P999",
            Name = "Missing",
            Price = 1m,
            Stock = 0,
            Description = "Missing product"
        });
        var priceAffected = repository.UpdateProductPrice(999, 2m);
        var deleteAffected = repository.DeleteProduct(999);

        Assert.Equal(0, updateAffected);
        Assert.Equal(0, priceAffected);
        Assert.Equal(0, deleteAffected);
    }

    [Fact]
    public void DeleteProduct_RemovesExistingProduct()
    {
        using var database = new SqliteTestDatabase();
        var repository = database.Repository;
        repository.AddProduct(CreateProduct());
        var saved = Assert.Single(repository.GetAllProducts());

        var affected = repository.DeleteProduct(saved.Id);

        Assert.Equal(1, affected);
        Assert.Null(repository.GetProductById(saved.Id));
        Assert.Empty(repository.GetAllProducts());
    }

    [Fact]
    public void AddProduct_WithCaseInsensitiveDuplicateCode_WrapsSqliteException()
    {
        using var database = new SqliteTestDatabase();
        var repository = database.Repository;
        repository.AddProduct(CreateProduct());

        var duplicate = CreateProduct();
        duplicate.Code = "p001";

        var exception = Assert.Throws<DataAccessException>(
            () => repository.AddProduct(duplicate));

        Assert.Contains("新增商品失败", exception.Message);
        Assert.IsType<SQLiteException>(exception.InnerException);
        Assert.Single(repository.GetAllProducts());
    }

    [Fact]
    public void GetAllProducts_WhenTableDoesNotExist_WrapsSqliteException()
    {
        using var database = new SqliteTestDatabase(initialize: false);

        var exception = Assert.Throws<DataAccessException>(
            () => database.Repository.GetAllProducts());

        Assert.Contains("查询商品列表失败", exception.Message);
        Assert.IsType<SQLiteException>(exception.InnerException);
    }

    [Fact]
    public void Dispose_RemovesTemporaryDatabaseFile()
    {
        string databasePath;

        using (var database = new SqliteTestDatabase())
        {
            databasePath = database.DatabasePath;
            Assert.True(File.Exists(databasePath));
        }

        Assert.False(File.Exists(databasePath));
    }

    private static Product CreateProduct()
    {
        return new Product
        {
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        };
    }
}
