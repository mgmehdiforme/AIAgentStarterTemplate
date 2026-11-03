using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace AgentTemplate.Infrastructure.Filters;

public sealed partial class PromptRenderFilter : IPromptRenderFilter
{
    private readonly ILogger<PromptRenderFilter> _logger;

    // Common PII pattern (Credit Card Numbers 13-19 digits)
    [GeneratedRegex(@"\b(?:\d[ -]*?){13,16}\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardRegex();

    public PromptRenderFilter(ILogger<PromptRenderFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnPromptRenderAsync(
        PromptRenderContext context, 
        Func<PromptRenderContext, Task> next)
    {
        await next(context);

        if (!string.IsNullOrEmpty(context.RenderedPrompt))
        {
            // Sanitize sensitive patterns before dispatch to external LLM
            var sanitized = CreditCardRegex().Replace(context.RenderedPrompt, "[REDACTED_CC]");
            
            if (sanitized != context.RenderedPrompt)
            {
                _logger.LogInformation("PromptRenderFilter sanitized sensitive PII from rendered prompt.");
                context.RenderedPrompt = sanitized;
            }
        }
    }
}
