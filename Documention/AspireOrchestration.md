# Aspire Orchestration & Service Defaults

The starter template leverages **.NET Aspire 9** to provide cloud-native local orchestration, distributed telemetry, service discovery, and container life-cycle management.

---

## 1. AppHost Topology (`AgentTemplate.AppHost`)

The `AgentTemplate.AppHost` project serves as the orchestrator. It manages all dependencies required for local development without needing separate manual Docker commands:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// 1. Distributed Cache (Redis)
var cache = builder.AddRedis("cache");

// 2. Vector Database (Qdrant)
var vectorDb = builder.AddQdrant("vectordb");

// 3. Local LLM Engine (Ollama container + model pull)
var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithOpenWebUI();

var llama = ollama.AddModel("llama3.2");

// 4. API Host Service
var api = builder.AddProject<Projects.AgentTemplate_Api>("agent-api")
    .WithReference(cache)
    .WithReference(vectorDb)
    .WithReference(llama)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### Components Provisioned:
| Resource | Purpose | Aspire Integration |
| :--- | :--- | :--- |
| **Redis** | Distributed session caching, output caching, and rate-limiting stores. | `Aspire.Hosting.Redis` |
| **Qdrant** | High-performance vector database for semantic search and Retrieval-Augmented Generation (RAG). | `Aspire.Hosting.Qdrant` |
| **Ollama** | Local zero-cost LLM runtime with OpenWebUI interface. | `CommunityToolkit.Aspire.Hosting.Ollama` |
| **Agent API** | The core Web API running minimal endpoints and Semantic Kernel orchestrations. | `AddProject<Projects.AgentTemplate_Api>` |

---

## 2. Service Defaults (`AgentTemplate.ServiceDefaults`)

`AgentTemplate.ServiceDefaults` is a shared extension library consumed by all services in the topology. It standardizes platform capabilities:

### 2.1 Unified OpenTelemetry Instrumentation
Registers distributed tracing and metrics across standard libraries and Semantic Kernel:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddRuntimeInstrumentation()
               // GenAI Metrics
               .AddMeter("Microsoft.SemanticKernel*")
               .AddMeter("AgentTemplate.Core*");
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               // GenAI Traces
               .AddSource("Microsoft.SemanticKernel*")
               .AddSource("AgentTemplate.Core*");
    });
```

### 2.2 Health Probes
Exposes standard liveness and readiness endpoints:
- **`/alive`**: Liveness probe ensuring the web application process is responsive.
- **`/health`**: Readiness probe checking connectivity to downstream dependencies (database, cache, model endpoints).

### 2.3 Service Discovery & Resilience Defaults
Automatically wires `Microsoft.Extensions.ServiceDiscovery` and standard HTTP resilience handlers across all configured `HttpClient` instances.
