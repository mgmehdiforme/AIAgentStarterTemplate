using AgentTemplate.Application.Abstractions;
using AgentTemplate.Domain.Exceptions;
using AgentTemplate.Infrastructure.Filters;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NSubstitute;
using Xunit;

namespace AgentTemplate.Tests.Unit.Guardrails;

public class FilterTests
{
    [Fact]
    public void PromptRenderFilter_ShouldSanitizeCreditCardNumbers_FromRenderedPrompt()
    {
        var rawPrompt = "Payment info: 4532-1234-5678-9012 for test";

        var sanitized = PromptRenderFilter.SanitizePrompt(rawPrompt);

        sanitized.Should().Be("Payment info: [REDACTED_CC] for test");
    }

    [Fact]
    public async Task ToolInvocationAuditFilter_ShouldThrowUnauthorizedException_WhenUserLacksRequiredPermission()
    {
        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.UserId.Returns("unauthorized-user");
        userContext.TenantId.Returns("tenant-demo");
        userContext.HasPermission("Orders.Cancel").Returns(false);
        userContext.HasPermission("Admin").Returns(false);

        var filter = new ToolInvocationAuditFilter(NullLogger<ToolInvocationAuditFilter>.Instance, userContext);
        var kernel = new Kernel();
        kernel.FunctionInvocationFilters.Add(filter);

        var function = KernelFunctionFactory.CreateFromMethod(() => "cancelled", "CancelOrder");

        var act = async () => await kernel.InvokeAsync(function);

        await act.Should().ThrowAsync<UnauthorizedOperationException>()
            .WithMessage("*lacks required permissions*");
    }

    [Fact]
    public async Task ToolInvocationAuditFilter_ShouldAllowInvocation_WhenUserHasRequiredPermission()
    {
        var userContext = Substitute.For<ICurrentUserContext>();
        userContext.UserId.Returns("authorized-user");
        userContext.TenantId.Returns("tenant-demo");
        userContext.HasPermission("Orders.Cancel").Returns(true);

        var filter = new ToolInvocationAuditFilter(NullLogger<ToolInvocationAuditFilter>.Instance, userContext);
        var kernel = new Kernel();
        kernel.FunctionInvocationFilters.Add(filter);

        var function = KernelFunctionFactory.CreateFromMethod(() => "order-cancelled-success", "CancelOrder");

        var result = await kernel.InvokeAsync(function);

        result.GetValue<string>().Should().Be("order-cancelled-success");
    }
}
