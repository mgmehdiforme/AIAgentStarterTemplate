using System.Threading.RateLimiting;
using AgentTemplate.Api.Data;
using AgentTemplate.Api.Endpoints;
using AgentTemplate.Api.Middleware;
using AgentTemplate.Api.Services;
using AgentTemplate.Application.Abstractions;
using AgentTemplate.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire Service Defaults (OpenTelemetry, Service Discovery, Health Probes)
builder.AddServiceDefaults();

// Security Context (Ambient tenant & user identity)
builder.Services.AddScoped<CurrentUserContext>();
builder.Services.AddScoped<ICurrentUserContext>(sp => sp.GetRequiredService<CurrentUserContext>());

// Infrastructure Layer (AI Kernel, Plugins, Guardrails, EF Core, Resilience)
builder.Services.AddInfrastructure(builder.Configuration);

// Endpoint Rate Limiting for Generative AI routes
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("agent-policy", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() 
                ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                ?? "global",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 5
            }));
});

// OpenAPI & Modern Scalar Documentation UI
builder.Services.AddOpenApi();

var app = builder.Build();

// Service defaults endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Identity context middleware
app.UseMiddleware<CurrentUserMiddleware>();

// Rate limiting middleware
app.UseRateLimiter();

// OpenAPI & Scalar Explorer
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("AI Agent Starter Template API")
               .WithTheme(ScalarTheme.Moon);
    });
}

// Minimal API Route Groups
app.MapAgentEndpoints();
app.MapCatalogEndpoints();

// Initialize and seed database
await DatabaseSeeder.SeedAsync(app.Services);

app.Run();

// For WebApplicationFactory integration testing
public partial class Program { }
