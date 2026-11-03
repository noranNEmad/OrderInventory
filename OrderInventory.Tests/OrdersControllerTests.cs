using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrderInventory.Api.Controllers;
using OrderInventory.Api.Data;
using OrderInventory.Api.DTOs;
using OrderInventory.Api.Enums;
using OrderInventory.Api.Models;
using Microsoft.Data.Sqlite;

namespace OrderInventory.Tests;

public class OrdersControllerTests
{
    [Fact]
    public async Task Create_ValidOrder_CreatesOrderAndReducesStock()
    {
        // Arrange: Create SQLite in-memory database
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var mouse = new Product
        {
            Name = "Wireless Mouse",
            Price = 550,
            StockQuantity = 20
        };

        var keyboard = new Product
        {
            Name = "Mechanical Keyboard",
            Price = 100,
            StockQuantity = 10
        };

        context.Products.AddRange(mouse, keyboard);
        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
        {
            new()
            {
                ProductId = mouse.Id,
                Quantity = 2
            },
            new()
            {
                ProductId = keyboard.Id,
                Quantity = 3
            }
        }
        };

        // Act
        var result = await controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result.Result);

        var orderResponse =
            Assert.IsType<OrderResponseDto>(createdResult.Value);

        Assert.Equal(1400, orderResponse.TotalAmount);
        Assert.Equal(2, orderResponse.Items.Count);

        var updatedMouse = await context.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == mouse.Id);

        var updatedKeyboard = await context.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == keyboard.Id);

        Assert.Equal(18, updatedMouse!.StockQuantity);
        Assert.Equal(7, updatedKeyboard!.StockQuantity);

        Assert.Equal(1100, orderResponse.Items[0].Subtotal);
        Assert.Equal(300, orderResponse.Items[1].Subtotal);
    }

    [Fact]
    public async Task Create_InsufficientStock_ReturnsBadRequest()
    {
        // Arrange: Create SQLite in-memory database
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var mouse = new Product
        {
            Name = "Wireless Mouse",
            Price = 550,
            StockQuantity = 5
        };

        context.Products.Add(mouse);
        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
        {
            new()
            {
                ProductId = mouse.Id,
                Quantity = 10
            }
        }
        };

        // Act
        var result = await controller.Create(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(
            "Insufficient stock for product: Wireless Mouse",
            badRequest.Value);

        Assert.Empty(await context.Orders.ToListAsync());

        var unchangedMouse = await context.Products.FindAsync(mouse.Id);

        Assert.Equal(5, unchangedMouse!.StockQuantity);
    }

    [Fact]
    public async Task Create_NonExistingProduct_ReturnsBadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new AppDbContext(options);

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var newOrder = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
        {
            new CreateOrderItemDto
            {
                ProductId = 999,
                Quantity = 2
            }
        }
        };

        // Act
        var result = await controller.Create(newOrder);

        // Assert
        var badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(
            "One or more products do not exist.",
            badRequest.Value);

        // Verify no order was created
        Assert.Empty(await context.Orders.ToListAsync());
    }


    [Fact]
    public async Task Create_DuplicateProducts_ReturnsBadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var newOrder = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
        {
            new CreateOrderItemDto
            {
                ProductId = product.Id,
                Quantity = 2
            },
            new CreateOrderItemDto
            {
                ProductId = product.Id,
                Quantity = 3
            }
        }
        };

        // Act
        var result = await controller.Create(newOrder);

        // Assert
        var badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(
            "Each product can only appear once in an order.",
            badRequest.Value);

        Assert.Empty(await context.Orders.ToListAsync());

        var savedProduct = await context.Products.FindAsync(product.Id);

        Assert.NotNull(savedProduct);
        Assert.Equal(20, savedProduct.StockQuantity);
    }


    [Fact]
    public async Task GetById_ExistingOrder_ReturnsOrderWithItems()
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

        var order = new Order
        {
            Status = OrderStatus.Pending,
            TotalAmount = 1100,
            Items = new List<OrderItem>
        {
            new OrderItem
            {
                Product = product,
                Quantity = 2,
                UnitPrice = 550
            }
        }
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        // Act
        var result = await controller.GetById(order.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response = Assert.IsType<OrderResponseDto>(okResult.Value);

        Assert.Equal(order.Id, response.Id);
        Assert.Equal(OrderStatus.Pending, response.Status);
        Assert.Equal(1100m, response.TotalAmount);

        Assert.Single(response.Items);

        Assert.Equal(product.Id, response.Items[0].ProductId);
        Assert.Equal("Wireless Mouse", response.Items[0].ProductName);
        Assert.Equal(2, response.Items[0].Quantity);
        Assert.Equal(550m, response.Items[0].UnitPrice);
        Assert.Equal(1100m, response.Items[0].Subtotal);
    }

    [Fact]
    public async Task GetById_NonExistingOrder_ReturnsNotFound()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.GetById(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);

        Assert.Equal("Order not found.", notFound.Value);
    }

    [Fact]
    public async Task GetAll_ReturnsAllOrders()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        context.Orders.AddRange(
            new Order { TotalAmount = 100 },
            new Order { TotalAmount = 200 },
            new Order { TotalAmount = 300 }
        );

        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.GetAll(null, 1, 10);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PaginationResponseDto<OrderResponseDto>>(
            okResult.Value);

        Assert.Equal(3, response.TotalCount);
        Assert.Equal(1, response.TotalPages);
        Assert.Equal(3, response.Items.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsCorrectPage()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        context.Orders.AddRange(
            new Order { TotalAmount = 100 },
            new Order { TotalAmount = 200 },
            new Order { TotalAmount = 300 }
        );

        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.GetAll(null, 2, 2);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PaginationResponseDto<OrderResponseDto>>(
            okResult.Value);

        Assert.Equal(3, response.TotalCount);
        Assert.Equal(2, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(2, response.TotalPages);
        Assert.Single(response.Items);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsMatchingOrders()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        context.Orders.AddRange(
            new Order { TotalAmount = 100, Status = OrderStatus.Pending },
            new Order { TotalAmount = 200, Status = OrderStatus.Confirmed },
            new Order { TotalAmount = 300, Status = OrderStatus.Pending },
            new Order { TotalAmount = 400, Status = OrderStatus.Cancelled }
        );

        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.GetAll(OrderStatus.Pending, 1, 10);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PaginationResponseDto<OrderResponseDto>>(
            okResult.Value);

        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.Items.Count);

        Assert.All(response.Items, item =>
            Assert.Equal(OrderStatus.Pending, item.Status));
    }

    [Fact]
    public async Task Confirm_PendingOrder_ChangesStatusToConfirmed()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var order = new Order
        {
            Status = OrderStatus.Pending,
            TotalAmount = 100
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.Confirm(order.Id);

        Assert.IsType<OkObjectResult>(result);

        var updatedOrder = await context.Orders.FindAsync(order.Id);

        Assert.Equal(OrderStatus.Confirmed, updatedOrder!.Status);
    }

    [Fact]
    public async Task Confirm_AlreadyConfirmedOrder_ReturnsBadRequest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var order = new Order
        {
            Status = OrderStatus.Confirmed,
            TotalAmount = 100
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.Confirm(order.Id);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            "Only pending orders can be confirmed.",
            badRequest.Value);
    }

    [Fact]
    public async Task Cancel_PendingOrder_RestoresProductStock()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new AppDbContext(options);

        var product = new Product
        {
            Name = "Wireless Mouse",
            Price = 550,
            StockQuantity = 8
        };

        var order = new Order
        {
            Status = OrderStatus.Pending,
            TotalAmount = 1100,
            Items = new List<OrderItem>
        {
            new OrderItem
            {
                Product = product,
                Quantity = 2,
                UnitPrice = 550
            }
        }
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.Cancel(order.Id);

        Assert.IsType<OkObjectResult>(result);

        var updatedProduct = await context.Products.FindAsync(product.Id);
        var updatedOrder = await context.Orders.FindAsync(order.Id);

        Assert.Equal(10, updatedProduct!.StockQuantity);
        Assert.Equal(OrderStatus.Cancelled, updatedOrder!.Status);
    }
    [Fact]
    public async Task Cancel_NonExistingOrder_ReturnsNotFound()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new AppDbContext(options);

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.Cancel(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal("Order not found.", notFound.Value);
    }
    [Fact]
    public async Task Cancel_ConfirmedOrder_ReturnsBadRequest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new AppDbContext(options);

        var product = new Product
        {
            Name = "Wireless Mouse",
            Price = 550,
            StockQuantity = 8
        };

        var order = new Order
        {
            Status = OrderStatus.Confirmed,
            TotalAmount = 1100,
            Items = new List<OrderItem>
        {
            new OrderItem
            {
                Product = product,
                Quantity = 2,
                UnitPrice = 550
            }
        }
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.Cancel(order.Id);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            "Only pending orders can be cancelled.",
            badRequest.Value);

        var updatedProduct = await context.Products.FindAsync(product.Id);
        Assert.Equal(8, updatedProduct!.StockQuantity);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelledOrder_ReturnsBadRequest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new AppDbContext(options);

        var order = new Order
        {
            Status = OrderStatus.Cancelled,
            TotalAmount = 100
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.Cancel(order.Id);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal("Order is already cancelled.", badRequest.Value);
    }

    [Fact]
    public async Task Confirm_NonExistingOrder_ReturnsNotFound()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var controller = new OrdersController(
    context,
    NullLogger<OrdersController>.Instance);

        var result = await controller.Confirm(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal("Order not found.", notFound.Value);
    }
}
