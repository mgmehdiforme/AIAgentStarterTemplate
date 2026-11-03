# Semantic Kernel Integration & Multi-Model Routing

This document details how Microsoft Semantic Kernel 1.81 is structured, registered, and managed within the template.

---

## 1. Kernel Lifecycle: The Transient Pattern

### Why Singleton Kernels are an Anti-Pattern
Registering `Kernel` as a singleton in dependency injection causes severe production defects:
1. **Cross-Tenant State Leakage:** Plugin instances and execution contexts retained on the kernel can inadvertently expose sensitive data across concurrent requests.
2. **Memory Leaks:** Accumulated chat history, function call metadata, and filter states remain allocated indefinitely.
3. **Concurrency Race Conditions:** The mutable nature of plugins and filters on a shared instance leads to unpredictable behavior under load.

### Solution: Transient Scoped Factory
In this starter template, `Kernel` is registered as **Transient** and constructed on demand by `IKernelFactory`:

```csharp
// DependencyInjection.cs
services.AddTransient<Kernel>(sp =>
{
    var factory = sp.GetRequiredService<IKernelFactory>();
    return factory.CreateKernel();
});
```

Every incoming request or agent turn receives a fresh, lightweight `Kernel` instance populated with:
- Scoped plugins injected with the caller's specific `ICurrentUserContext`.
- Configured guardrail filters (`LoopCappingFilter`, `ToolInvocationAuditFilter`, `PromptRenderFilter`).
- Multi-model connector routing.

---

## 2. Multi-Model Routing via Service IDs

In complex agent workflows, executing every simple triage task on large, expensive reasoning models degrades performance and inflates token budgets.

The template registers two distinct service IDs:

```csharp
// Fast utility model (triage, intent classification, entity extraction)
ConfigureModelConnector(builder, _options.FastModel, "FastUtilityModel", httpClient);

// High-capacity reasoning model (multi-step planning, tool orchestration, final synthesis)
ConfigureModelConnector(builder, _options.ReasoningModel, "ReasoningModel", httpClient);
```

### Consuming Models Dynamically
Services retrieve specific models by name:
```csharp
var fastChat = kernel.GetRequiredService<IChatCompletionService>("FastUtilityModel");
var reasoningChat = kernel.GetRequiredService<IChatCompletionService>("ReasoningModel");
```

---

## 3. Dual Provider Configuration: Ollama vs. Cloud

The template natively supports both local inference via Ollama and cloud inference via OpenAI or Azure OpenAI using the `AI:Provider` configuration switch in `appsettings.json`:

```csharp
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
    // Compatible with OpenAI and local Ollama OpenAI-compatible /v1 endpoints
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
```

---

## 4. Version-Controlled External Prompts

All system prompts and agent instructions are decoupled from C# source code and managed inside the `prompts/` folder:

```
prompts/
├── System/
│   └── persona.md               # Core agent persona & operational guardrails
├── IntentClassification/
│   └── prompt.yaml              # Rapid intent classification template
└── OrderPlanning/
    └── prompt.yaml              # Multi-step tool action planning template
```

`PromptTemplateManager` dynamically resolves and loads these files at runtime, allowing prompt engineers to tweak prompt formulations without recompiling the codebase.
