using System.Net;
using System.Net.Http.Json;
using AgentTemplate.Application.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AgentTemplate.Tests.Integration;

public class AgentApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AgentApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AliveProbe_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/alive");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthProbe_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task ProductsEndpoint_ShouldReturnSeededProducts()
    {
        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        products.Should().NotBeNull();
        products!.Count.Should().BeGreaterOrEqualTo(3);
        products.Should().Contain(p => p.Sku == "PROD-001");
    }

    [Fact]
    public async Task OrdersEndpoint_ShouldReturnOrders_ForDefaultTenantAndUser()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orders");
        request.Headers.Add("X-Tenant-Id", "tenant-default");
        request.Headers.Add("X-User-Id", "user-demo");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();

        orders.Should().NotBeNull();
        orders!.Count.Should().BeGreaterOrEqualTo(1);
        orders.Should().Contain(o => o.CustomerId == "user-demo");
    }

    [Fact]
    public async Task OrdersEndpoint_ShouldEnforceMultiTenancyIsolation_WhenDifferentTenantSupplied()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orders");
        request.Headers.Add("X-Tenant-Id", "tenant-isolated-xyz");
        request.Headers.Add("X-User-Id", "user-demo");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();

        orders.Should().NotBeNull();
        orders!.Should().BeEmpty(); // Cross-tenant access prevented by global query filter
    }
}
