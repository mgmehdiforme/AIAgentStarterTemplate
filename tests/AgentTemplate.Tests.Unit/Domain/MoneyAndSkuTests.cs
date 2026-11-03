using AgentTemplate.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AgentTemplate.Tests.Unit.Domain;

public class MoneyAndSkuTests
{
    [Fact]
    public void Money_ShouldInitializeCorrectly_WhenValidAmountAndCurrency()
    {
        var money = new Money(149.99m, "usd");

        money.Amount.Should().Be(149.99m);
        money.Currency.Should().Be("USD");
        money.ToString().Should().Be("149.99 USD");
    }

    [Fact]
    public void Money_ShouldThrowException_WhenAmountIsNegative()
    {
        var act = () => new Money(-10m, "USD");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Sku_ShouldNormalizeToUpper_WhenValidString()
    {
        var sku = new Sku("  prod-001  ");
        sku.Value.Should().Be("PROD-001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Sku_ShouldThrowException_WhenEmpty(string? invalidSku)
    {
        var act = () => new Sku(invalidSku!);
        act.Should().Throw<ArgumentException>();
    }
}
