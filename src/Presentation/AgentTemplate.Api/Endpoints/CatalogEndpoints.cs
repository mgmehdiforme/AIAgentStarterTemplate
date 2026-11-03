using AgentTemplate.Application.Abstractions;
using AgentTemplate.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace AgentTemplate.Api.Endpoints;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1")
            .WithTags("Catalog & Orders");

        group.MapGet("/products", async (
            [FromQuery] string? query,
            [FromServices] IProductRepository productRepository,
            CancellationToken cancellationToken) =>
        {
            var results = await productRepository.SearchAsync(query ?? string.Empty, 20, cancellationToken);
            var dtos = results.Select(p => new ProductDto(
                p.Id,
                p.Sku.Value,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.StockQuantity,
                p.IsActive)).ToList();

            return Results.Ok(dtos);
        })
        .WithName("SearchProducts")
        .WithSummary("Retrieves products matching a search query in the current tenant.");

        group.MapGet("/orders", async (
            [FromServices] IOrderRepository orderRepository,
            [FromServices] ICurrentUserContext userContext,
            CancellationToken cancellationToken) =>
        {
            var orders = await orderRepository.GetByCustomerIdAsync(userContext.UserId, cancellationToken);
            var dtos = orders.Select(order => new OrderDto(
                order.Id,
                order.CustomerId,
                order.Status.ToString(),
                order.TotalAmount.Amount,
                order.TotalAmount.Currency,
                order.CreatedAt,
                order.Items.Select(i => new OrderItemDto(
                    i.ProductId,
                    i.ProductName,
                    i.Sku.Value,
                    i.Quantity,
                    i.UnitPrice.Amount,
                    i.LineTotal.Amount)).ToList())).ToList();

            return Results.Ok(dtos);
        })
        .WithName("GetMyOrders")
        .WithSummary("Retrieves all orders for the current customer.");

        return group;
    }
}
