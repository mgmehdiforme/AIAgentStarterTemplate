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

    public static string SanitizePrompt(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
            return prompt;

        return CreditCardRegex().Replace(prompt, "[REDACTED_CC]");
    }

    public async Task OnPromptRenderAsync(
        PromptRenderContext context, 
        Func<PromptRenderContext, Task> next)
    {
        await next(context);

        if (!string.IsNullOrEmpty(context.RenderedPrompt))
        {
            // Sanitize sensitive patterns before dispatch to external LLM
            var sanitized = SanitizePrompt(context.RenderedPrompt);
            
            if (sanitized != context.RenderedPrompt)
            {
                _logger.LogInformation("PromptRenderFilter sanitized sensitive PII from rendered prompt.");
                context.RenderedPrompt = sanitized;
            }
        }
    }
}
