using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using OrderInventory.Api.DTOs.Auth;
using Xunit;

namespace OrderInventory.Tests.IntegrationTests;

[Collection("IntegrationTests")]
public class RateLimitingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RateLimitingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthEndpoint_WhenLimitExceeded_Returns429TooManyRequests()
    {
        var strictFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:AuthPermitLimit"] = "2",
                    ["RateLimiting:AuthWindowSeconds"] = "60"
                });
            });
        });

        var client = strictFactory.CreateClient();

        var loginDto = new LoginDto
        {
            Email = "rate_limit_test@example.com",
            Password = "WrongPassword123!"
        };

        // Requests 1 & 2 pass through the limiter to the endpoint (returning 401 Unauthorized)
        var response1 = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        var response2 = await client.PostAsJsonAsync("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.Unauthorized, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response2.StatusCode);

        // Request 3 exceeds permit limit and must be rejected with 429 TooManyRequests
        var response3 = await client.PostAsJsonAsync("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.TooManyRequests, response3.StatusCode);
    }
}

