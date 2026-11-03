using AgentTemplate.Infrastructure.Configuration;
using AgentTemplate.Infrastructure.Filters;
using AgentTemplate.Infrastructure.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace AgentTemplate.Infrastructure.AI;

public interface IKernelFactory
{
    Kernel CreateKernel();
}

public sealed class KernelFactory : IKernelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AiOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public const string FastUtilityModelServiceId = "FastUtilityModel";
    public const string ReasoningModelServiceId = "ReasoningModel";

    public KernelFactory(
        IServiceProvider serviceProvider,
        IOptions<AiOptions> options,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    public Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(_loggerFactory);

        var httpClient = _httpClientFactory.CreateClient("InferenceClient");

        // Multi-Model Routing: Fast Utility Model
        ConfigureModelConnector(builder, _options.FastModel, FastUtilityModelServiceId, httpClient);

        // Multi-Model Routing: Reasoning Model
        ConfigureModelConnector(builder, _options.ReasoningModel, ReasoningModelServiceId, httpClient);

        // Build transient kernel instance
        var kernel = builder.Build();

        // Attach Plugins from scoped DI provider
        var orderPlugin = (OrderPlugin?)_serviceProvider.GetService(typeof(OrderPlugin));
        var productPlugin = (ProductPlugin?)_serviceProvider.GetService(typeof(ProductPlugin));
        if (orderPlugin != null)
        {
            kernel.Plugins.AddFromObject(orderPlugin, "OrderOperations");
        }
        if (productPlugin != null)
        {
            kernel.Plugins.AddFromObject(productPlugin, "ProductCatalog");
        }

        // Attach Guardrail Filters
        var loopFilter = (LoopCappingFilter?)_serviceProvider.GetService(typeof(LoopCappingFilter));
        var auditFilter = (ToolInvocationAuditFilter?)_serviceProvider.GetService(typeof(ToolInvocationAuditFilter));
        var renderFilter = (PromptRenderFilter?)_serviceProvider.GetService(typeof(PromptRenderFilter));

        if (loopFilter != null)
        {
            kernel.AutoFunctionInvocationFilters.Add(loopFilter);
        }
        if (auditFilter != null)
        {
            kernel.FunctionInvocationFilters.Add(auditFilter);
        }
        if (renderFilter != null)
        {
            kernel.PromptRenderFilters.Add(renderFilter);
        }

        return kernel;
    }

    private void ConfigureModelConnector(
        IKernelBuilder builder, 
        ModelOptions modelOptions, 
        string serviceId, 
        HttpClient httpClient)
    {
        var modelId = string.IsNullOrWhiteSpace(modelOptions.ModelId) ? "llama3.2" : modelOptions.ModelId;
        var apiKey = string.IsNullOrWhiteSpace(modelOptions.ApiKey) ? "unused" : modelOptions.ApiKey;

        if (_options.Provider == AiProviderType.AzureOpenAI)
        {
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: modelOptions.DeploymentName,
                endpoint: modelOptions.Endpoint,
                apiKey: apiKey,
                serviceId: serviceId,
                httpClient: httpClient);
        }
        else
        {
            // Works for OpenAI and Ollama (via OpenAI-compatible /v1 endpoint)
            if (!string.IsNullOrWhiteSpace(modelOptions.Endpoint))
            {
                builder.AddOpenAIChatCompletion(
                    modelId: modelId,
                    endpoint: new Uri(modelOptions.Endpoint),
                    apiKey: apiKey,
                    serviceId: serviceId,
                    httpClient: httpClient);
            }
            else
            {
                builder.AddOpenAIChatCompletion(
                    modelId: modelId,
                    apiKey: apiKey,
                    serviceId: serviceId,
                    httpClient: httpClient);
            }
        }
    }
}
