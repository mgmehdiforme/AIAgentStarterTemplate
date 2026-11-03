using AgentTemplate.Application.Abstractions;

namespace AgentTemplate.Api.Services;

public sealed class CurrentUserContext : ICurrentUserContext
{
    public string TenantId { get; set; } = "tenant-default";
    public string UserId { get; set; } = "user-demo";
    public IReadOnlyList<string> Roles { get; set; } = ["Customer", "Orders.Cancel"];
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    public bool HasPermission(string permission)
    {
        return Roles.Contains(permission, StringComparer.OrdinalIgnoreCase) ||
               Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
    }
}
