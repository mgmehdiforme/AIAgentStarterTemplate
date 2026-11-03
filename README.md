# Production-Grade .NET 9 AI Agent Starter Template

[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![.NET Aspire](https://img.shields.io/badge/Aspire-9.0-blue.svg)](https://learn.microsoft.com/dotnet/aspire/)
[![Semantic Kernel](https://img.shields.io/badge/Semantic_Kernel-1.81.0-darkgreen.svg)](https://learn.microsoft.com/semantic-kernel/)
[![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-orange.svg)](https://opentelemetry.io/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A production-ready, enterprise-grade starter template for autonomous AI agents and generative AI microservices built on **.NET 9**, **.NET Aspire**, **Microsoft Semantic Kernel**, **OpenTelemetry**, and **Polly Resilience**.

Designed to replace naive AI demo scripts with an enterprise architecture featuring **Clean Architecture**, **Zero-Trust Security Boundaries**, **Multi-Model Routing**, **Transient Kernel Lifecycle Management**, and **Autonomous Guardrail Filters**.

---

## 🌟 Key Features

- **.NET Aspire 9 Cloud-Native Orchestration:** Automatically provisions and connects Redis distributed cache, Qdrant vector database, and local Ollama container engine.
- **Clean Architecture & Framework Decoupling:** Core domain and application logic have **zero** dependencies on Semantic Kernel or external AI SDKs.
- **Transient Kernel Lifecycle:** Prevents cross-tenant context bleeding and memory leakage by constructing ephemeral, scoped kernel instances.
- **Multi-Model Routing:** Routes fast triage queries to `FastUtilityModel` and complex multi-step reasoning to `ReasoningModel`.
- **Dual AI Provider Support:** Easily toggle between local Ollama (zero API cost / offline) and cloud providers (OpenAI / Azure OpenAI) via configuration.
- **Production Guardrail Filters:**
  - `LoopCappingFilter`: Halts infinite tool loops exceeding 5 iterations with `AgentLoopExhaustedException`.
  - `ToolInvocationAuditFilter`: Enforces RBAC permissions for sensitive operations and logs duration audits.
  - `PromptRenderFilter`: Sanitizes rendered prompts and redacts PII before dispatch to external inference endpoints.
- **Zero-Trust Identity Isolation:** Plugin tools resolve tenant and user identity from ambient `ICurrentUserContext`, strictly preventing LLM parameter spoofing.
- **Global Multi-Tenancy Query Filters:** Enforced at the EF Core level across all data access.
- **Real-Time Streaming:** Server-Sent Events (SSE) endpoint emitting real-time agent thoughts, tool calls, and text deltas.
- **Resilient HTTP Inferences:** Polly pipelines with exponential backoff, jitter, circuit breaker, and timeout budgets.
- **Interactive API Documentation:** Modern Scalar UI explorer available at `/scalar/v1`.

---

## 📂 Repository Layout

```text
├── Documention/                                # Comprehensive Documentation Guides
│   ├── README.md                               # Documentation Index
│   ├── Quickstart.md                           # Setup & Running Guide
│   ├── Architecture.md                         # Clean Architecture & Security
│   ├── AspireOrchestration.md                  # .NET Aspire & Service Defaults
│   ├── SemanticKernelIntegration.md            # SK Lifecycle & Multi-Model Routing
│   ├── GuardrailFilters.md                     # Loop Capping & Safety Filters
│   └── Testing.md                              # Testing Strategy & Execution
│
├── prompts/                                    # Version-controlled prompt assets
│   ├── System/persona.md                       # Core agent persona & guardrails
│   ├── IntentClassification/prompt.yaml        # Query intent classification
│   └── OrderPlanning/prompt.yaml               # Tool planning template
│
├── src/
│   ├── Aspire/
│   │   └── AgentTemplate.AppHost/              # Aspire Orchestrator (Ollama, Redis, Qdrant, API)
│   ├── Shared/
│   │   └── AgentTemplate.ServiceDefaults/      # OpenTelemetry, Health Checks (/health, /alive), Discovery
│   ├── Core/
│   │   ├── AgentTemplate.Domain/               # Pure Entities (Order, Product), Value Objects (Money, Sku)
│   │   └── AgentTemplate.Application/          # Interfaces (IAgentWorkflowOrchestrator), DTOs, Contracts
│   ├── Infrastructure/
│   │   └── AgentTemplate.Infrastructure/       # Semantic Kernel, EF Core, Guardrails, Plugins, Polly
│   └── Presentation/
│       └── AgentTemplate.Api/                  # Minimal APIs, SSE Streaming, Rate Limiting, Middleware
│
└── tests/
    ├── AgentTemplate.Tests.Unit/               # Domain, Value Objects, and Filter Unit Tests
    └── AgentTemplate.Tests.Integration/        # WebApplicationFactory Headless Integration Tests
```

---

## 🚀 Quickstart

### 1. Run with .NET Aspire
Spins up the full topology (Redis, Qdrant, Ollama, and API):
```bash
dotnet run --project src/Aspire/AgentTemplate.AppHost/AgentTemplate.AppHost.csproj
```

### 2. Run Standalone API
```bash
dotnet run --project src/Presentation/AgentTemplate.Api/AgentTemplate.Api.csproj
```
Open **Scalar UI** at: `http://localhost:5000/scalar/v1`

### 3. Run Automated Tests
```bash
dotnet test AgentTemplate.sln
```

---

## 📖 In-Depth Documentation

For detailed architectural runbooks, setup steps, and security guidelines, explore the **[Documention](Documention/)** directory:
- [Quickstart Guide](Documention/Quickstart.md)
- [Architecture & Clean Design](Documention/Architecture.md)
- [Aspire Orchestration](Documention/AspireOrchestration.md)
- [Semantic Kernel Integration](Documention/SemanticKernelIntegration.md)
- [Guardrail Filters & Safety](Documention/GuardrailFilters.md)
- [Testing & Verification](Documention/Testing.md)

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
