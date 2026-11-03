# Testing & Verification Strategy

The template includes an extensive, enterprise-grade automated test suite divided into **Unit Tests** and **Headless Integration Tests**.

---

## 1. Running All Tests

To execute all tests across the solution:

```bash
dotnet test AgentTemplate.sln
```

Expected output:
```
Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13 - AgentTemplate.Tests.Unit.dll
Passed!  - Failed: 0, Passed:  5, Skipped: 0, Total:  5 - AgentTemplate.Tests.Integration.dll
```

---

## 2. Unit Test Suite (`AgentTemplate.Tests.Unit`)

Focuses on pure domain logic, value objects, exceptions, and guardrail filters in complete isolation using **xUnit**, **FluentAssertions**, and **NSubstitute**.

| Test Fixture | Purpose |
| :--- | :--- |
| `MoneyAndSkuTests` | Validates value object immutability, normalization, currency formatting, and defensive bounds checking (negative amounts). |
| `OrderAndProductTests` | Verifies aggregate root invariants, line-item totals calculation, order cancellation state transitions, and stock depletion checks. |
| `FilterTests` | Tests `PromptRenderFilter` sanitization of PII, and `ToolInvocationAuditFilter` authorization validation and rejection of unauthorized calls. |

---

## 3. Integration Test Suite (`AgentTemplate.Tests.Integration`)

Leverages **`WebApplicationFactory<Program>`** to spin up an in-memory test server executing the actual ASP.NET Core middleware, dependency injection pipeline, rate limiters, and EF Core SQLite context.

| Integration Test | Tested Flow |
| :--- | :--- |
| `AliveProbe_ShouldReturnHealthy` | Confirms the `/alive` liveness probe returns HTTP 200 with content `"Healthy"`. |
| `HealthProbe_ShouldReturnHealthy` | Confirms the `/health` readiness probe returns HTTP 200. |
| `ProductsEndpoint_ShouldReturnSeededProducts` | Verifies `/api/v1/products` returns products initialized by `DatabaseSeeder`. |
| `OrdersEndpoint_ShouldReturnOrders_ForDefaultTenantAndUser` | Verifies `/api/v1/orders` resolves the authenticated user claims from HTTP headers. |
| `OrdersEndpoint_ShouldEnforceMultiTenancyIsolation` | Verifies cross-tenant data leakage is prevented when requests supply a different `X-Tenant-Id`. |

---

## 4. Writing New Tests

### Adding a New Unit Test
Place tests in `tests/AgentTemplate.Tests.Unit/{Layer}/`. Follow the Arrange-Act-Assert (AAA) pattern:
```csharp
[Fact]
public void Should_PerformExpectedAction_When_Condition()
{
    // Arrange
    var order = new Order(Guid.NewGuid(), "tenant-test", "user-1");

    // Act
    order.Cancel("Reason");

    // Assert
    order.Status.Should().Be(OrderStatus.Cancelled);
}
```

### Adding a New Endpoint Integration Test
Inherit from `IClassFixture<WebApplicationFactory<Program>>`:
```csharp
public class MyNewIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MyNewIntegrationTest(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Endpoint_ShouldReturnSuccess()
    {
        var response = await _client.GetAsync("/my-endpoint");
        response.EnsureSuccessStatusCode();
    }
}
```
