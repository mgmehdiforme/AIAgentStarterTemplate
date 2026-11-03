namespace AgentTemplate.Application.Dtos;

public sealed record AgentWorkflowRequest(
    string SessionId,
    string UserMessage,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record AgentWorkflowResult(
    string SessionId,
    string FinalResponse,
    int TotalTokensUsed,
    int ExecutionStepsCount,
    bool CompletedSuccessfully,
    IReadOnlyList<AgentStepAuditLog> StepAudits);

public sealed record AgentStepAuditLog(
    int StepIndex,
    string StepType,
    string ActionName,
    string Details,
    long DurationMilliseconds);

public abstract record AgentWorkflowStreamEvent(string SessionId, DateTimeOffset Timestamp);

public sealed record AgentThoughtStreamEvent(
    string SessionId, 
    string Thought, 
    DateTimeOffset Timestamp) : AgentWorkflowStreamEvent(SessionId, Timestamp);

public sealed record AgentToolCallStreamEvent(
    string SessionId, 
    string PluginName, 
    string FunctionName, 
    string Arguments, 
    DateTimeOffset Timestamp) : AgentWorkflowStreamEvent(SessionId, Timestamp);

public sealed record AgentDeltaStreamEvent(
    string SessionId, 
    string Delta, 
    DateTimeOffset Timestamp) : AgentWorkflowStreamEvent(SessionId, Timestamp);

public sealed record AgentCompletedStreamEvent(
    string SessionId, 
    string FinalResponse, 
    int TotalTokensUsed, 
    DateTimeOffset Timestamp) : AgentWorkflowStreamEvent(SessionId, Timestamp);

public sealed record AgentErrorStreamEvent(
    string SessionId, 
    string ErrorMessage, 
    DateTimeOffset Timestamp) : AgentWorkflowStreamEvent(SessionId, Timestamp);
