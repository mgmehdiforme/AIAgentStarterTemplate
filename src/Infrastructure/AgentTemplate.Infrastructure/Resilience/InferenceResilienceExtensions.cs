using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace AgentTemplate.Infrastructure.Resilience;

public static class InferenceResilienceExtensions
{
    public static IHttpClientBuilder AddInferenceResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            // Per-attempt timeout for LLM inference calls
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);

            // Cumulative timeout budget across all retries
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);

            // Jittered exponential backoff for HTTP 429 (Rate Limit) and HTTP 5xx (Inference Server Overload)
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromSeconds(2);

            // Circuit breaker sampling duration must be >= 2 * AttemptTimeout
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(90);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 8;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        });

        return builder;
    }
}
