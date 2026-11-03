using System.Runtime.CompilerServices;
using AgentTemplate.Application.Abstractions;
using AgentTemplate.Application.Dtos;
using AgentTemplate.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgentTemplate.Infrastructure.AI;

public sealed class SemanticKernelWorkflowOrchestrator : IAgentWorkflowOrchestrator
{
    private readonly IKernelFactory _kernelFactory;
    private readonly IPromptTemplateManager _promptManager;
    private readonly ICurrentUserContext _userContext;
    private readonly ILogger<SemanticKernelWorkflowOrchestrator> _logger;

    public SemanticKernelWorkflowOrchestrator(
        IKernelFactory kernelFactory,
        IPromptTemplateManager promptManager,
        ICurrentUserContext userContext,
        ILogger<SemanticKernelWorkflowOrchestrator> logger)
    {
        _kernelFactory = kernelFactory;
        _promptManager = promptManager;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<AgentWorkflowResult> ExecuteWorkflowAsync(
        AgentWorkflowRequest request, 
        CancellationToken cancellationToken = default)
    {
        var audits = new List<AgentStepAuditLog>();
        var kernel = _kernelFactory.CreateKernel();
        var chatService = kernel.GetRequiredService<IChatCompletionService>(KernelFactory.ReasoningModelServiceId);

        var systemPrompt = await _promptManager.GetPromptAsync("System/persona", cancellationToken);
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage($"{systemPrompt}\nAuthenticated User: {_userContext.UserId}, Tenant: {_userContext.TenantId}");
        chatHistory.AddUserMessage(request.UserMessage);

        var executionSettings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        try
        {
            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel,
                cancellationToken);

            var totalTokens = 150; // default estimated tokens

            return new AgentWorkflowResult(
                request.SessionId,
                response.Content ?? string.Empty,
                totalTokens,
                chatHistory.Count,
                true,
                audits);
        }
        catch (AgentLoopExhaustedException ex)
        {
            _logger.LogError(ex, "Agent loop exhausted during execution for session {SessionId}", request.SessionId);
            return new AgentWorkflowResult(
                request.SessionId,
                $"Execution halted: Agent exceeded maximum loop threshold of {ex.MaxIterations} steps.",
                0,
                ex.MaxIterations,
                false,
                audits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure during agent workflow execution for session {SessionId}", request.SessionId);
            throw;
        }
    }

    public async IAsyncEnumerable<AgentWorkflowStreamEvent> StreamWorkflowAsync(
        AgentWorkflowRequest request, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var kernel = _kernelFactory.CreateKernel();
        var chatService = kernel.GetRequiredService<IChatCompletionService>(KernelFactory.ReasoningModelServiceId);

        var systemPrompt = await _promptManager.GetPromptAsync("System/persona", cancellationToken);
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage($"{systemPrompt}\nAuthenticated User: {_userContext.UserId}, Tenant: {_userContext.TenantId}");
        chatHistory.AddUserMessage(request.UserMessage);

        yield return new AgentThoughtStreamEvent(
            request.SessionId, 
            "Analyzing user request and evaluating available tools...", 
            DateTimeOffset.UtcNow);

        var executionSettings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        IAsyncEnumerable<StreamingChatMessageContent>? streamResponse = null;
        AgentErrorStreamEvent? errorEvent = null;

        try
        {
            streamResponse = chatService.GetStreamingChatMessageContentsAsync(
                chatHistory,
                executionSettings,
                kernel,
                cancellationToken);
        }
        catch (AgentLoopExhaustedException ex)
        {
            errorEvent = new AgentErrorStreamEvent(
                request.SessionId,
                $"Loop cap exceeded: {ex.Message}",
                DateTimeOffset.UtcNow);
        }

        if (errorEvent != null)
        {
            yield return errorEvent;
            yield break;
        }

        if (streamResponse != null)
        {
            var fullResponseBuffer = new System.Text.StringBuilder();

            await foreach (var chunk in streamResponse.WithCancellation(cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    fullResponseBuffer.Append(chunk.Content);
                    yield return new AgentDeltaStreamEvent(
                        request.SessionId,
                        chunk.Content,
                        DateTimeOffset.UtcNow);
                }
            }

            yield return new AgentCompletedStreamEvent(
                request.SessionId,
                fullResponseBuffer.ToString(),
                fullResponseBuffer.Length / 4,
                DateTimeOffset.UtcNow);
        }
    }
}
