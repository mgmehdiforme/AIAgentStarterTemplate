using AgentTemplate.Api.Services;

namespace AgentTemplate.Api.Middleware;

public sealed class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CurrentUserContext userContext)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) && !string.IsNullOrWhiteSpace(tenantHeader))
        {
            userContext.TenantId = tenantHeader.ToString().Trim();
        }

        if (context.Request.Headers.TryGetValue("X-User-Id", out var userHeader) && !string.IsNullOrWhiteSpace(userHeader))
        {
            userContext.UserId = userHeader.ToString().Trim();
        }

        if (context.Request.Headers.TryGetValue("X-User-Roles", out var rolesHeader) && !string.IsNullOrWhiteSpace(rolesHeader))
        {
            userContext.Roles = rolesHeader.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        await _next(context);
    }
}
