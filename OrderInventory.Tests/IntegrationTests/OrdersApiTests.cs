
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderInventory.Api.DTOs;

namespace OrderInventory.Tests.IntegrationTests;

[Collection("IntegrationTests")]
public class OrdersApiTests :
    IAsyncLifetime
{
    private readonly HttpClient _client;

    public OrdersApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Login automatically before each test
    public Task InitializeAsync()
    {
        return AuthenticateAsync();
    }

    // Dispose HttpClient after each test
    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    // Authenticate and store JWT token
    private async Task AuthenticateAsync()
    {
        var loginRequest = new
        {
            email = "integration-admin@example.com",
            password = "Test1234"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result.Token);
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    [Fact]
    public async Task CreateOrder_ValidOrder_ReturnsCreated()
    {
        // Arrange: create a product first
        var newProduct = new
        {
            name = "Order Integration Product",
            price = 100.00m,
            stockQuantity = 20,
            categoryId = (int?)null
        };

        var productResponse = await _client.PostAsJsonAsync(
            "/api/Products", newProduct);

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var product = await productResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(product);

        // Act: create an order
        var newOrder = new
        {
            items = new[]
            {
                new
                {
                    productId = product.Id,
                    quantity = 2
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync(
            "/api/Orders", newOrder);

        // Assert
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_ReducesProductStock()
    {
        // Arrange: create a product with 20 items
        var newProduct = new
        {
            name = "Stock Reduction Test Product",
            price = 100.00m,
            stockQuantity = 20,
            categoryId = (int?)null
        };

        var productResponse = await _client.PostAsJsonAsync(
            "/api/Products", newProduct);

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var product = await productResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(product);

        // Act: order 2 items
        var newOrder = new
        {
            items = new[]
            {
                new
                {
                    productId = product.Id,
                    quantity = 2
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync(
            "/api/Orders", newOrder);

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        // Assert: retrieve product and check stock
        var updatedResponse = await _client.GetAsync(
            $"/api/Products/{product.Id}");

        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);

        var updatedProduct = await updatedResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(updatedProduct);
        Assert.Equal(18, updatedProduct.StockQuantity);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStock_ReturnsBadRequest()
    {
        // Arrange: create a product with 20 items
        var newProduct = new
        {
            name = "Insufficient Stock Test Product",
            price = 100.00m,
            stockQuantity = 20,
            categoryId = (int?)null
        };

        var productResponse = await _client.PostAsJsonAsync(
            "/api/Products", newProduct);

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var product = await productResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(product);

        // Act: try to order 25 items
        var newOrder = new
        {
            items = new[]
            {
                new
                {
                    productId = product.Id,
                    quantity = 25
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync(
            "/api/Orders", newOrder);

        // Assert: order should be rejected
        Assert.Equal(
            HttpStatusCode.BadRequest,
            orderResponse.StatusCode);

        // Verify that stock is still 20
        var updatedResponse = await _client.GetAsync(
            $"/api/Products/{product.Id}");

        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);

        var updatedProduct = await updatedResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(updatedProduct);
        Assert.Equal(20, updatedProduct.StockQuantity);
    }

    [Fact]
    public async Task CancelOrder_RestoresProductStock()
    {
        // Arrange: create a product with 20 items
        var newProduct = new
        {
            name = "Cancel Order Test Product",
            price = 100.00m,
            stockQuantity = 20,
            categoryId = (int?)null
        };

        var productResponse = await _client.PostAsJsonAsync(
            "/api/Products", newProduct);

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var product = await productResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(product);

        // Create an order for 2 items
        var newOrder = new
        {
            items = new[]
            {
                new
                {
                    productId = product.Id,
                    quantity = 2
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync(
            "/api/Orders", newOrder);

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        // Verify stock decreased to 18
        var stockAfterOrderResponse = await _client.GetAsync(
            $"/api/Products/{product.Id}");

        var stockAfterOrder = await stockAfterOrderResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(stockAfterOrder);
        Assert.Equal(18, stockAfterOrder.StockQuantity);

        // Act: cancel the order
        var createdOrder = await orderResponse.Content
            .ReadFromJsonAsync<OrderResponseDto>();

        Assert.NotNull(createdOrder);

        var cancelResponse = await _client.PutAsync(
            $"/api/Orders/{createdOrder.Id}/cancel", null);

        // Assert: cancellation succeeded
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        // Verify stock restored to 20
        var finalStockResponse = await _client.GetAsync(
            $"/api/Products/{product.Id}");

        var finalProduct = await finalStockResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(finalProduct);
        Assert.Equal(20, finalProduct.StockQuantity);
    }

    [Fact]
    public async Task CancelOrder_Twice_ReturnsBadRequestAndDoesNotRestoreStockAgain()
    {
        // Arrange: create product
        var newProduct = new
        {
            name = "Double Cancel Test Product",
            price = 100.00m,
            stockQuantity = 20,
            categoryId = (int?)null
        };

        var productResponse = await _client.PostAsJsonAsync(
            "/api/Products", newProduct);

        var product = await productResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(product);

        // Create order for 2 items
        var newOrder = new
        {
            items = new[]
            {
                new
                {
                    productId = product.Id,
                    quantity = 2
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync(
            "/api/Orders", newOrder);

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        var order = await orderResponse.Content
            .ReadFromJsonAsync<OrderResponseDto>();

        Assert.NotNull(order);

        // First cancellation
        var firstCancel = await _client.PutAsync(
            $"/api/Orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, firstCancel.StatusCode);

        // Second cancellation
        var secondCancel = await _client.PutAsync(
            $"/api/Orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.BadRequest, secondCancel.StatusCode);

        // Verify stock is still 20
        var finalResponse = await _client.GetAsync(
            $"/api/Products/{product.Id}");

        var finalProduct = await finalResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(finalProduct);
        Assert.Equal(20, finalProduct.StockQuantity);
    }

    [Fact]
    public async Task ConfirmOrder_ValidOrder_ReturnsOk()
    {
        // Arrange: create product
        var newProduct = new
        {
            name = "Confirm Order Test Product",
            price = 100.00m,
            stockQuantity = 20,
            categoryId = (int?)null
        };

        var productResponse = await _client.PostAsJsonAsync(
            "/api/Products", newProduct);

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var product = await productResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(product);

        // Create order
        var newOrder = new
        {
            items = new[]
            {
                new
                {
                    productId = product.Id,
                    quantity = 2
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync(
            "/api/Orders", newOrder);

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        var order = await orderResponse.Content
            .ReadFromJsonAsync<OrderResponseDto>();

        Assert.NotNull(order);

        // Act: confirm order
        var confirmResponse = await _client.PutAsync(
            $"/api/Orders/{order.Id}/confirm", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        // Verify order status
        var getOrderResponse = await _client.GetAsync(
            $"/api/Orders/{order.Id}");

        var confirmedOrder = await getOrderResponse.Content
            .ReadFromJsonAsync<OrderResponseDto>();

        Assert.NotNull(confirmedOrder);
        Assert.Equal("Confirmed", confirmedOrder.Status.ToString());
    }

    [Fact]
    public async Task ConfirmOrder_Twice_ReturnsBadRequest()
    {
        // Arrange: create product
        var newProduct = new
        {
            name = "Double Confirm Test Product",
            price = 100.00m,
            stockQuantity = 20,
            categoryId = (int?)null
        };

        var productResponse = await _client.PostAsJsonAsync(
            "/api/Products", newProduct);

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var product = await productResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(product);

        // Create order
        var newOrder = new
        {
            items = new[]
            {
                new
                {
                    productId = product.Id,
                    quantity = 2
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync(
            "/api/Orders", newOrder);

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        var order = await orderResponse.Content
            .ReadFromJsonAsync<OrderResponseDto>();

        Assert.NotNull(order);

        // First confirmation
        var firstConfirm = await _client.PutAsync(
            $"/api/Orders/{order.Id}/confirm", null);

        Assert.Equal(HttpStatusCode.OK, firstConfirm.StatusCode);

        // Second confirmation
        var secondConfirm = await _client.PutAsync(
            $"/api/Orders/{order.Id}/confirm", null);

        Assert.Equal(HttpStatusCode.BadRequest, secondConfirm.StatusCode);
    }

    [Fact]
    public async Task CancelConfirmedOrder_ReturnsBadRequest()
    {
        // Arrange: create product
        var newProduct = new
        {
            name = "Cancel Confirmed Test Product",
            price = 100.00m,
            stockQuantity = 20,
            categoryId = (int?)null
        };

        var productResponse = await _client.PostAsJsonAsync(
            "/api/Products", newProduct);

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var product = await productResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(product);

        // Create order
        var newOrder = new
        {
            items = new[]
            {
                new
                {
                    productId = product.Id,
                    quantity = 2
                }
            }
        };

        var orderResponse = await _client.PostAsJsonAsync(
            "/api/Orders", newOrder);

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        var order = await orderResponse.Content
            .ReadFromJsonAsync<OrderResponseDto>();

        Assert.NotNull(order);

        // Confirm order
        var confirmResponse = await _client.PutAsync(
            $"/api/Orders/{order.Id}/confirm", null);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        // Act: try to cancel confirmed order
        var cancelResponse = await _client.PutAsync(
            $"/api/Orders/{order.Id}/cancel", null);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            cancelResponse.StatusCode);
    }
}
