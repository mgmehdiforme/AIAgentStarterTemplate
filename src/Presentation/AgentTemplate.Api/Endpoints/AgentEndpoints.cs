using System.Text.Json;
using AgentTemplate.Application.Abstractions;
using AgentTemplate.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace AgentTemplate.Api.Endpoints;

public static class AgentEndpoints
{
    public static RouteGroupBuilder MapAgentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/agent")
            .WithTags("AI Agent");

        // Synchronous Chat Endpoint
        group.MapPost("/chat", async (
            [FromBody] ChatRequestDto request,
            [FromServices] IAgentWorkflowOrchestrator orchestrator,
            CancellationToken cancellationToken) =>
        {
            var workflowRequest = new AgentWorkflowRequest(
                string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString() : request.SessionId,
                request.UserMessage);

            var result = await orchestrator.ExecuteWorkflowAsync(workflowRequest, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("AgentChat")
        .WithSummary("Executes an autonomous AI Agent workflow synchronously and returns the complete result.")
        .RequireRateLimiting("agent-policy");

        // Server-Sent Events (SSE) Streaming Endpoint
        group.MapPost("/chat/stream", async (
            [FromBody] ChatRequestDto request,
            [FromServices] IAgentWorkflowOrchestrator orchestrator,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
            httpContext.Response.Headers.Append("Cache-Control", "no-cache");
            httpContext.Response.Headers.Append("Connection", "keep-alive");

            var workflowRequest = new AgentWorkflowRequest(
                string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString() : request.SessionId,
                request.UserMessage);

            var stream = orchestrator.StreamWorkflowAsync(workflowRequest, cancellationToken);

            await foreach (var streamEvent in stream.WithCancellation(cancellationToken))
            {
                var eventType = streamEvent.GetType().Name;
                var json = JsonSerializer.Serialize(streamEvent, streamEvent.GetType());

                await httpContext.Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
        })
        .WithName("AgentChatStream")
        .WithSummary("Streams real-time agent thoughts, tool invocations, and token deltas via Server-Sent Events (SSE).")
        .RequireRateLimiting("agent-policy");

        return group;
    }
}

public sealed record ChatRequestDto(string UserMessage, string? SessionId = null);
