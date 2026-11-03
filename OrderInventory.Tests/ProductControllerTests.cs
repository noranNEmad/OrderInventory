
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.Controllers;
using OrderInventory.Api.Data;
using OrderInventory.Api.DTOs;
using OrderInventory.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace OrderInventory.Tests;

public class ProductControllerTests
{
    [Fact]
    public async Task GetById_ExistingProduct_ReturnsProduct()
    {
        // Arrange: Prepare the database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var product = new Product
        {
            Name = "Wireless Mouse",
            Price = 550,
            StockQuantity = 20
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var controller = new ProductsController(context);

        // Act: Call the controller method
        var result = await controller.GetById(product.Id);

        // Assert: Check the response
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var returnedProduct =
            Assert.IsType<ProductResponseDto>(okResult.Value);

        Assert.Equal(product.Id, returnedProduct.Id);
        Assert.Equal("Wireless Mouse", returnedProduct.Name);
        Assert.Equal(550m, returnedProduct.Price);
        Assert.Equal(20, returnedProduct.StockQuantity);
    }


    [Fact]
    public async Task GetById_NonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var controller = new ProductsController(context);

        // Act
        var result = await controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }


    [Fact]
    public async Task GetAll_ReturnsPaginatedProducts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        context.Products.AddRange(
            new Product { Name = "Mouse", Price = 550, StockQuantity = 10 },
            new Product { Name = "Keyboard", Price = 800, StockQuantity = 15 },
            new Product { Name = "Monitor", Price = 3000, StockQuantity = 5 }
        );

        await context.SaveChangesAsync();

        var controller = new ProductsController(context);

        // Act
        var result = await controller.GetAll(page: 1, pageSize: 2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response =
            Assert.IsType<PaginationResponseDto<ProductResponseDto>>(okResult.Value);

        Assert.Equal(3, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
        Assert.Equal(1, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(2, response.Items.Count);
    }


    [Fact]
    public async Task GetAll_WithCategoryId_ReturnsOnlyProductsInThatCategory()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var electronics = new Category { Name = "Electronics" };
        var furniture = new Category { Name = "Furniture" };

        context.Categories.AddRange(electronics, furniture);
        await context.SaveChangesAsync();

        context.Products.AddRange(
            new Product
            {
                Name = "Mouse",
                Price = 550,
                StockQuantity = 10,
                CategoryId = electronics.Id
            },
            new Product
            {
                Name = "Keyboard",
                Price = 800,
                StockQuantity = 15,
                CategoryId = electronics.Id
            },
            new Product
            {
                Name = "Office Chair",
                Price = 3000,
                StockQuantity = 5,
                CategoryId = furniture.Id
            }
        );

        await context.SaveChangesAsync();

        var controller = new ProductsController(context);

        // Act
        var result = await controller.GetAll(categoryId: electronics.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response =
            Assert.IsType<PaginationResponseDto<ProductResponseDto>>(okResult.Value);

        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.Items.Count);

        Assert.All(response.Items, product =>
            Assert.Equal(electronics.Id, product.CategoryId));
    }



    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(-1, 10)]
    public async Task GetAll_InvalidPagination_ReturnsBadRequest(
        int page,
        int pageSize)
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var controller = new ProductsController(context);

        // Act
        var result = await controller.GetAll(page, pageSize);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }


    [Fact]
    public async Task Create_ValidProduct_ReturnsCreatedAndSavesProduct()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var controller = new ProductsController(context);

        var newProduct = new CreateProductDto
        {
            Name = "Gaming Mouse",
            Price = 750,
            StockQuantity = 25,
            CategoryId = null
        };

        // Act
        var result = await controller.Create(newProduct);

        // Assert: Check HTTP response
        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result.Result);

        var response =
            Assert.IsType<ProductResponseDto>(createdResult.Value);

        Assert.Equal("Gaming Mouse", response.Name);
        Assert.Equal(750m, response.Price);
        Assert.Equal(25, response.StockQuantity);

        // Assert: Check database
        var savedProduct = await context.Products
            .FirstOrDefaultAsync(p => p.Name == "Gaming Mouse");

        Assert.NotNull(savedProduct);
        Assert.Equal(750m, savedProduct.Price);
    }


    [Fact]
    public async Task Create_InvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var controller = new ProductsController(context);

        var newProduct = new CreateProductDto
        {
            Name = "Gaming Mouse",
            Price = 750,
            StockQuantity = 25,
            CategoryId = 999
        };

        // Act
        var result = await controller.Create(newProduct);

        // Assert
        var badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Category does not exist.", badRequest.Value);

        // Make sure no product was saved
        Assert.Empty(await context.Products.ToListAsync());
    }


    [Fact]
    public async Task Update_ExistingProduct_UpdatesAndReturnsProduct()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var product = new Product
        {
            Name = "Old Mouse",
            Price = 500,
            StockQuantity = 10
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var controller = new ProductsController(context);

        var updatedProduct = new UpdateProductDto
        {
            Name = "New Mouse",
            Price = 750,
            StockQuantity = 20,
            CategoryId = null
        };

        // Act
        var result = await controller.Update(product.Id, updatedProduct);

        // Assert: Check HTTP response
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response =
            Assert.IsType<ProductResponseDto>(okResult.Value);

        Assert.Equal("New Mouse", response.Name);
        Assert.Equal(750m, response.Price);
        Assert.Equal(20, response.StockQuantity);

        // Assert: Check database
        var savedProduct = await context.Products.FindAsync(product.Id);

        Assert.NotNull(savedProduct);
        Assert.Equal("New Mouse", savedProduct.Name);
        Assert.Equal(750m, savedProduct.Price);
        Assert.Equal(20, savedProduct.StockQuantity);
    }


    [Fact]
    public async Task Update_NonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var controller = new ProductsController(context);

        var updatedProduct = new UpdateProductDto
        {
            Name = "New Mouse",
            Price = 750,
            StockQuantity = 20,
            CategoryId = null
        };

        // Act
        var result = await controller.Update(999, updatedProduct);

        // Assert
        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(result.Result);

        Assert.Equal("Product not found.", notFoundResult.Value);
    }


    [Fact]
    public async Task Update_InvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var product = new Product
        {
            Name = "Old Mouse",
            Price = 500,
            StockQuantity = 10
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var controller = new ProductsController(context);

        var updatedProduct = new UpdateProductDto
        {
            Name = "New Mouse",
            Price = 750,
            StockQuantity = 20,
            CategoryId = 999
        };

        // Act
        var result = await controller.Update(product.Id, updatedProduct);

        // Assert
        var badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Category does not exist.", badRequest.Value);

        // Verify original product was not changed
        var savedProduct = await context.Products.FindAsync(product.Id);

        Assert.NotNull(savedProduct);
        Assert.Equal("Old Mouse", savedProduct.Name);
        Assert.Equal(500m, savedProduct.Price);
        Assert.Equal(10, savedProduct.StockQuantity);
    }


    [Fact]
    public async Task Purchase_ValidQuantity_ReducesStockAndReturnsProduct()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var product = new Product
        {
            Name = "Wireless Mouse",
            Price = 550,
            StockQuantity = 20
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var controller = new ProductsController(context);

        var request = new StockUpdateDto
        {
            Quantity = 5
        };

        // Act
        var result = await controller.Purchase(product.Id, request);

        // Assert: Check HTTP response
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response =
            Assert.IsType<ProductResponseDto>(okResult.Value);

        Assert.Equal(15, response.StockQuantity);

        // Assert: Check database
        var savedProduct = await context.Products.FindAsync(product.Id);

        Assert.NotNull(savedProduct);
        Assert.Equal(15, savedProduct.StockQuantity);
    }


    [Fact]
    public async Task Purchase_InsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var product = new Product
        {
            Name = "Wireless Mouse",
            Price = 550,
            StockQuantity = 10
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var controller = new ProductsController(context);

        var request = new StockUpdateDto
        {
            Quantity = 15
        };

        // Act
        var result = await controller.Purchase(product.Id, request);

        // Assert
        var badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Insufficient stock.", badRequest.Value);

        // Verify stock was not changed
        var savedProduct = await context.Products.FindAsync(product.Id);

        Assert.NotNull(savedProduct);
        Assert.Equal(10, savedProduct.StockQuantity);
    }


    [Fact]
    public async Task Purchase_NonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var controller = new ProductsController(context);

        var request = new StockUpdateDto
        {
            Quantity = 5
        };

        // Act
        var result = await controller.Purchase(999, request);

        // Assert
        var notFound =
            Assert.IsType<NotFoundObjectResult>(result.Result);

        Assert.Equal("Product not found.", notFound.Value);

        // Verify database is still empty
        Assert.Empty(await context.Products.ToListAsync());
    }


    [Fact]
    public async Task Restock_ValidQuantity_IncreasesStockAndReturnsProduct()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var product = new Product
        {
            Name = "Wireless Mouse",
            Price = 550,
            StockQuantity = 10
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var controller = new ProductsController(context);

        var request = new StockUpdateDto
        {
            Quantity = 5
        };

        // Act
        var result = await controller.Restock(product.Id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response =
            Assert.IsType<ProductResponseDto>(okResult.Value);

        Assert.Equal(15, response.StockQuantity);

        var savedProduct = await context.Products.FindAsync(product.Id);

        Assert.NotNull(savedProduct);
        Assert.Equal(15, savedProduct.StockQuantity);
    }

    [Fact]
    public async Task Restock_NonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var controller = new ProductsController(context);

        var request = new StockUpdateDto
        {
            Quantity = 5
        };

        // Act
        var result = await controller.Restock(999, request);

        // Assert
        var notFound =
            Assert.IsType<NotFoundObjectResult>(result.Result);

        Assert.Equal("Product not found.", notFound.Value);

        Assert.Empty(await context.Products.ToListAsync());
    }
    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    [InlineData(1000001, false)]
    public void StockUpdateDto_QuantityValidation(
    int quantity,
    bool isValid)
    {
        // Arrange
        var dto = new StockUpdateDto
        {
            Quantity = quantity
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValidResult = Validator.TryValidateObject(
            dto,
            context,
            results,
            validateAllProperties: true);

        // Assert
        Assert.Equal(isValid, isValidResult);
    }
}
