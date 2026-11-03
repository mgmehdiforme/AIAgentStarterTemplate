using System.Diagnostics;
using AgentTemplate.Application.Abstractions;
using AgentTemplate.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace AgentTemplate.Infrastructure.Filters;

public sealed class ToolInvocationAuditFilter : IFunctionInvocationFilter
{
    private readonly ILogger<ToolInvocationAuditFilter> _logger;
    private readonly ICurrentUserContext _userContext;

    public ToolInvocationAuditFilter(
        ILogger<ToolInvocationAuditFilter> logger, 
        ICurrentUserContext userContext)
    {
        _logger = logger;
        _userContext = userContext;
    }

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context, 
        Func<FunctionInvocationContext, Task> next)
    {
        var plugin = context.Function.PluginName ?? "Global";
        var functionName = context.Function.Name;

        // Security check: Check if function is marked sensitive or requires specific permissions
        var isSensitive = functionName.Contains("Cancel", StringComparison.OrdinalIgnoreCase) ||
                          functionName.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                          plugin.Equals("SensitiveOperations", StringComparison.OrdinalIgnoreCase);

        if (isSensitive && !_userContext.HasPermission("Orders.Cancel") && !_userContext.HasPermission("Admin"))
        {
            _logger.LogWarning(
                "Unauthorized tool invocation attempt: User '{UserId}' in Tenant '{TenantId}' attempted '{Plugin}.{Function}'",
                _userContext.UserId,
                _userContext.TenantId,
                plugin,
                functionName);

            throw new UnauthorizedOperationException(
                $"{plugin}.{functionName}",
                _userContext.UserId,
                $"User '{_userContext.UserId}' lacks required permissions to invoke '{plugin}.{functionName}'.");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Audit: Tool '{Plugin}.{Function}' executed in {ElapsedMs}ms by User '{UserId}' (Tenant: '{TenantId}')",
                plugin,
                functionName,
                stopwatch.ElapsedMilliseconds,
                _userContext.UserId,
                _userContext.TenantId);
        }
    }
}
