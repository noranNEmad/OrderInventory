using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.Data;
using OrderInventory.Api.Models;

namespace OrderInventory.Tests;

public class ProductTests
{
    [Fact]
    public async Task AddProduct_ShouldSaveProductToDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var product = new Product
        {
            Name = "Test Mouse",
            Price = 550,
            StockQuantity = 10
        };

        // Act
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Assert
        var savedProduct = await context.Products
            .FirstOrDefaultAsync(p => p.Name == "Test Mouse");

        Assert.NotNull(savedProduct);
        Assert.Equal("Test Mouse", savedProduct.Name);
        Assert.Equal(550m, savedProduct.Price);
        Assert.Equal(10, savedProduct.StockQuantity);
    }
}