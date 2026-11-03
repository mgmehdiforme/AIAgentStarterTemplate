namespace AgentTemplate.Application.Abstractions;

public interface ICurrentUserContext
{
    string TenantId { get; }
    string UserId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}
