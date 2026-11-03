using AgentTemplate.Domain.Entities;
using AgentTemplate.Domain.Exceptions;
using AgentTemplate.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AgentTemplate.Tests.Unit.Domain;

public class OrderAndProductTests
{
    [Fact]
    public void Order_ShouldCalculateCorrectTotal_WhenMultipleItemsAdded()
    {
        var order = new Order(Guid.NewGuid(), "tenant-test", "user-123");
        var sku1 = new Sku("SKU-1");
        var sku2 = new Sku("SKU-2");

        order.AddItem(Guid.NewGuid(), "Product A", sku1, 2, new Money(50m, "USD"));
        order.AddItem(Guid.NewGuid(), "Product B", sku2, 1, new Money(30m, "USD"));

        order.TotalAmount.Amount.Should().Be(130m);
        order.TotalAmount.Currency.Should().Be("USD");
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void Order_ShouldCancelSuccessfully_WhenPending()
    {
        var order = new Order(Guid.NewGuid(), "tenant-test", "user-123");
        order.Cancel("Customer requested cancellation");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Customer requested cancellation");
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Product_ShouldReduceStock_WhenSufficientQuantityAvailable()
    {
        var product = new Product(
            Guid.NewGuid(), 
            "tenant-test", 
            new Sku("SKU-100"), 
            "Test Device", 
            "Description", 
            new Money(99m, "USD"), 
            10);

        product.ReduceStock(3);
        product.StockQuantity.Should().Be(7);
    }

    [Fact]
    public void Product_ShouldThrowDomainException_WhenStockIsInsufficient()
    {
        var product = new Product(
            Guid.NewGuid(), 
            "tenant-test", 
            new Sku("SKU-100"), 
            "Test Device", 
            "Description", 
            new Money(99m, "USD"), 
            2);

        var act = () => product.ReduceStock(5);
        act.Should().Throw<DomainException>()
           .WithMessage("*Insufficient stock*");
    }
}
