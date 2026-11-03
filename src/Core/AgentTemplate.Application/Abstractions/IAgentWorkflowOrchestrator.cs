using AgentTemplate.Application.Dtos;

namespace AgentTemplate.Application.Abstractions;

public interface IAgentWorkflowOrchestrator
{
    Task<AgentWorkflowResult> ExecuteWorkflowAsync(
        AgentWorkflowRequest request, 
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<AgentWorkflowStreamEvent> StreamWorkflowAsync(
        AgentWorkflowRequest request, 
        CancellationToken cancellationToken = default);
}
