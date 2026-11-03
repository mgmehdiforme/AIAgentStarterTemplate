namespace AgentTemplate.Application.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsActive);

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderDto(
    Guid Id,
    string CustomerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items);
