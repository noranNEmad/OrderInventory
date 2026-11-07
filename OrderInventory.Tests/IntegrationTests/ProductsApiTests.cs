
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OrderInventory.Api.DTOs;

namespace OrderInventory.Tests.IntegrationTests;

[Collection("IntegrationTests")]
public class ProductsApiTests :
    IAsyncLifetime
{
    private readonly HttpClient _client;

    public ProductsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // تسجيل الدخول تلقائيًا قبل كل اختبار
    public async Task InitializeAsync()
    {
        await AuthenticateAsync();
    }

    // إغلاق HttpClient بعد انتهاء الاختبارات
    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

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

        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.IsSuccessStatusCode,
            $"Login failed. Status: {response.StatusCode}. Response: {responseContent}");

        using var jsonDocument = JsonDocument.Parse(responseContent);

        var token = jsonDocument.RootElement
            .GetProperty("token")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetProducts_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/Products");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CreateProduct_ValidProduct_ReturnsCreated()
    {
        var newProduct = new
        {
            name = "Integration Test Product",
            price = 150.00m,
            stockQuantity = 20,
            categoryId = (int?)null
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Products",
            newProduct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ThenGetProduct_ReturnsCreatedProduct()
    {
        var newProduct = new
        {
            name = "Integration Product Check",
            price = 200.00m,
            stockQuantity = 15,
            categoryId = (int?)null
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/Products",
            newProduct);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdProduct = await createResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(createdProduct);

        var getResponse = await _client.GetAsync(
            $"/api/Products/{createdProduct.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var product = await getResponse.Content
            .ReadFromJsonAsync<ProductResponseDto>();

        Assert.NotNull(product);
        Assert.Equal("Integration Product Check", product.Name);
        Assert.Equal(200.00m, product.Price);
    }

    [Fact]
    public async Task CreateProduct_InvalidPrice_ReturnsBadRequest()
    {
        var invalidProduct = new
        {
            name = "Invalid Product",
            price = -50.00m,
            stockQuantity = 10,
            categoryId = (int?)null
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Products",
            invalidProduct);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetProduct_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/Products/999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}
