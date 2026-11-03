using AgentTemplate.Domain.Common;
using AgentTemplate.Domain.Exceptions;
using AgentTemplate.Domain.ValueObjects;

namespace AgentTemplate.Domain.Entities;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

public sealed class OrderItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public Sku Sku { get; private set; } = default!;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = default!;

    public Money LineTotal => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

    private OrderItem() { } // EF Core

    public OrderItem(Guid id, Guid productId, string productName, Sku sku, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        Id = id;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

public sealed class Order : Entity<Guid>, IAggregateRoot
{
    private readonly List<OrderItem> _items = [];

    public string TenantId { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Money TotalAmount
    {
        get
        {
            if (_items.Count == 0)
                return Money.Zero();

            var currency = _items[0].UnitPrice.Currency;
            var total = _items.Sum(i => i.LineTotal.Amount);
            return new Money(total, currency);
        }
    }

    private Order() { } // EF Core

    public Order(Guid id, string tenantId, string customerId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("CustomerId is required.", nameof(customerId));

        Id = id;
        TenantId = tenantId;
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AddItem(Guid productId, string productName, Sku sku, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Cannot add items to order with status {Status}.");

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            _items.Remove(existing);
            _items.Add(new OrderItem(existing.Id, productId, productName, sku, existing.Quantity + quantity, unitPrice));
        }
        else
        {
            _items.Add(new OrderItem(Guid.NewGuid(), productId, productName, sku, quantity, unitPrice));
        }
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Order cannot be confirmed from status {Status}.");

        Status = OrderStatus.Confirmed;
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Delivered || Status == OrderStatus.Cancelled)
            throw new DomainException($"Cannot cancel order in status {Status}.");

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
        CancellationReason = reason;
    }
}
