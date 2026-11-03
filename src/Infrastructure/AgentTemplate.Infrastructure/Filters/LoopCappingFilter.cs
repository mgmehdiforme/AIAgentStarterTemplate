using AgentTemplate.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace AgentTemplate.Infrastructure.Filters;

public sealed class LoopCappingFilter : IAutoFunctionInvocationFilter
{
    private readonly int _maxAllowedIterations;
    private readonly ILogger<LoopCappingFilter> _logger;

    public LoopCappingFilter(ILogger<LoopCappingFilter> logger, int maxAllowedIterations = 5)
    {
        _logger = logger;
        _maxAllowedIterations = maxAllowedIterations;
    }

    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context, 
        Func<AutoFunctionInvocationContext, Task> next)
    {
        _logger.LogDebug(
            "AutoFunctionInvocation loop request turn {RequestSequenceIndex}, function {FunctionSequenceIndex} of {FunctionCount} (max limit: {MaxAllowedIterations})",
            context.RequestSequenceIndex + 1,
            context.FunctionSequenceIndex + 1,
            context.FunctionCount,
            _maxAllowedIterations);

        // Check if loop count has reached or exceeded max iterations limit
        if (context.RequestSequenceIndex >= _maxAllowedIterations)
        {
            context.Terminate = true;
            _logger.LogWarning(
                "Agent execution loop capped: Reached {RequestSequenceIndex} iterations (limit: {MaxIterations})", 
                context.RequestSequenceIndex, 
                _maxAllowedIterations);

            throw new AgentLoopExhaustedException(
                $"Agent execution halted: Exceeded maximum allowed tool loops ({_maxAllowedIterations}).",
                _maxAllowedIterations);
        }

        await next(context);
    }
}
