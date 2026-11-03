using AgentTemplate.Application.Abstractions;
using AgentTemplate.Infrastructure.AI;
using AgentTemplate.Infrastructure.Configuration;
using AgentTemplate.Infrastructure.Filters;
using AgentTemplate.Infrastructure.Persistence;
using AgentTemplate.Infrastructure.Plugins;
using AgentTemplate.Infrastructure.Prompts;
using AgentTemplate.Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace AgentTemplate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Options
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        var aiOptions = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

        // Database / Persistence
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=agent_template.db";
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IProductRepository, EfProductRepository>();

        // Resilient Inference HTTP Client
        services.AddHttpClient("InferenceClient")
            .AddInferenceResilience();

        // Guardrail Filters
        services.AddScoped<LoopCappingFilter>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LoopCappingFilter>>();
            return new LoopCappingFilter(logger, aiOptions.MaxAllowedToolLoops);
        });
        services.AddScoped<ToolInvocationAuditFilter>();
        services.AddScoped<PromptRenderFilter>();

        // Plugins
        services.AddScoped<OrderPlugin>();
        services.AddScoped<ProductPlugin>();

        // AI Services & Lifecycle
        services.AddSingleton<IPromptTemplateManager, PromptTemplateManager>();
        services.AddScoped<IKernelFactory, KernelFactory>();

        // Transient Kernel Registration
        services.AddTransient<Kernel>(sp =>
        {
            var factory = sp.GetRequiredService<IKernelFactory>();
            return factory.CreateKernel();
        });

        // Application Workflow Orchestrator
        services.AddScoped<IAgentWorkflowOrchestrator, SemanticKernelWorkflowOrchestrator>();

        return services;
    }
}
