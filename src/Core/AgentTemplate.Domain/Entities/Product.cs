using AgentTemplate.Domain.Common;
using AgentTemplate.Domain.Exceptions;
using AgentTemplate.Domain.ValueObjects;

namespace AgentTemplate.Domain.Entities;

public sealed class Product : Entity<Guid>, IAggregateRoot
{
    public string TenantId { get; private set; } = string.Empty;
    public Sku Sku { get; private set; } = default!;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = default!;
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }

    private Product() { } // EF Core

    public Product(Guid id, string tenantId, Sku sku, string name, string description, Money price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock cannot be negative.");

        Id = id;
        TenantId = tenantId;
        Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price ?? throw new ArgumentNullException(nameof(price));
        StockQuantity = stockQuantity;
        IsActive = true;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (StockQuantity < quantity)
            throw new DomainException($"Insufficient stock for product '{Name}' (SKU: {Sku}). Requested: {quantity}, Available: {StockQuantity}");

        StockQuantity -= quantity;
    }

    public void Restock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        StockQuantity += quantity;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
