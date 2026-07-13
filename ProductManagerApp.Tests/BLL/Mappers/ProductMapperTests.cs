using ProductManagerApp.BLL.Mappers;
using ProductManagerApp.DTO;
using ProductManagerApp.Entity;

namespace ProductManagerApp.Tests.BLL.Mappers;

/// <summary>
/// 验证创建、更新和查询模型之间的映射不会遗漏或改变字段。
/// </summary>
public class ProductMapperTests
{
    [Fact]
    public void ToEntity_FromCreateDto_MapsAllFields()
    {
        var dto = new ProductCreateDto
        {
            Code = "P001",
            Name = "Phone",
            Price = 1999m,
            Stock = 10,
            Description = "Flagship phone"
        };

        var product = ProductMapper.ToEntity(dto);

        Assert.Equal(dto.Code, product.Code);
        Assert.Equal(dto.Name, product.Name);
        Assert.Equal(dto.Price, product.Price);
        Assert.Equal(dto.Stock, product.Stock);
        Assert.Equal(dto.Description, product.Description);
    }

    [Fact]
    public void ToEntity_FromUpdateDto_MapsAllFields()
    {
        var dto = new ProductUpdateDto
        {
            Id = 7,
            Code = "P007",
            Name = "Laptop",
            Price = 6999m,
            Stock = 5,
            Description = "Portable computer"
        };

        var product = ProductMapper.ToEntity(dto);

        Assert.Equal(dto.Id, product.Id);
        Assert.Equal(dto.Code, product.Code);
        Assert.Equal(dto.Name, product.Name);
        Assert.Equal(dto.Price, product.Price);
        Assert.Equal(dto.Stock, product.Stock);
        Assert.Equal(dto.Description, product.Description);
    }

    [Fact]
    public void ToQueryDto_FromProduct_MapsAllFields()
    {
        var product = new Product
        {
            Id = 3,
            Code = "P003",
            Name = "Keyboard",
            Price = 299m,
            Stock = 20,
            Description = "Mechanical keyboard"
        };

        var dto = ProductMapper.ToQueryDto(product);

        Assert.Equal(product.Id, dto.Id);
        Assert.Equal(product.Code, dto.Code);
        Assert.Equal(product.Name, dto.Name);
        Assert.Equal(product.Price, dto.Price);
        Assert.Equal(product.Stock, dto.Stock);
        Assert.Equal(product.Description, dto.Description);
    }
}
