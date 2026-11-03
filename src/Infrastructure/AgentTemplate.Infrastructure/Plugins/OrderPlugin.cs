using System.ComponentModel;
using AgentTemplate.Application.Abstractions;
using AgentTemplate.Application.Dtos;
using Microsoft.SemanticKernel;

namespace AgentTemplate.Infrastructure.Plugins;

public sealed class OrderPlugin
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserContext _userContext;

    public OrderPlugin(IOrderRepository orderRepository, ICurrentUserContext userContext)
    {
        _orderRepository = orderRepository;
        _userContext = userContext;
    }

    [KernelFunction, Description("Retrieves detailed order information by order ID. Only returns orders belonging to the authenticated customer.")]
    public async Task<OrderDto?> GetOrderAsync(
        [Description("The unique identifier (GUID) of the order")] string orderId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(orderId, out var parsedId))
            return null;

        var order = await _orderRepository.GetByIdAsync(parsedId, cancellationToken);
        if (order == null || order.CustomerId != _userContext.UserId)
            return null;

        return new OrderDto(
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
                i.LineTotal.Amount)).ToList());
    }

    [KernelFunction, Description("Retrieves all recent orders for the currently authenticated customer.")]
    public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(_userContext.UserId, cancellationToken);

        return orders.Select(order => new OrderDto(
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
    }

    [KernelFunction, Description("Cancels an existing order for the current customer if it has not yet been delivered.")]
    public async Task<string> CancelOrderAsync(
        [Description("The unique identifier (GUID) of the order to cancel")] string orderId,
        [Description("The reason for order cancellation")] string reason,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(orderId, out var parsedId))
            return "Invalid order ID format.";

        var order = await _orderRepository.GetByIdAsync(parsedId, cancellationToken);
        if (order == null || order.CustomerId != _userContext.UserId)
            return $"Order '{orderId}' was not found.";

        try
        {
            order.Cancel(reason);
            await _orderRepository.UpdateAsync(order, cancellationToken);
            return $"Order '{orderId}' has been successfully cancelled.";
        }
        catch (Exception ex)
        {
            return $"Failed to cancel order: {ex.Message}";
        }
    }
}
