# Production-Grade .NET 9 AI Agent Starter Template

Welcome to the **.NET AI Agent Starter Template**. This repository provides an enterprise-ready, production-grade foundation for autonomous AI agents and generative AI microservices built with **.NET 9**, **.NET Aspire**, **Microsoft Semantic Kernel**, **OpenTelemetry**, and **Polly Resilience**.

---

## 📚 Documentation Index

All detailed guides and architectural runbooks are organized in this `Documention` directory:

| Document | Description |
| :--- | :--- |
| **[Quickstart](Quickstart.md)** | Step-by-step instructions to clone, configure, build, and run the agent template locally with or without Docker. |
| **[Architecture & Clean Design](Architecture.md)** | Complete breakdown of the Clean Architecture layers, zero-trust security boundaries, and multi-tenancy isolation. |
| **[Aspire Orchestration](AspireOrchestration.md)** | Guide to .NET Aspire AppHost managing Redis, Qdrant vector database, and local Ollama inference containers. |
| **[Semantic Kernel Integration](SemanticKernelIntegration.md)** | Transient Kernel lifecycle management, multi-model routing (`FastUtilityModel` vs `ReasoningModel`), and external prompt templates. |
| **[Guardrail Filters & Safety](GuardrailFilters.md)** | Enforcing loop limits (`LoopCappingFilter`), tool execution authorization (`ToolInvocationAuditFilter`), and prompt PII sanitization (`PromptRenderFilter`). |
| **[Testing & Verification](Testing.md)** | Comprehensive testing strategy covering unit tests and headless integration tests via `WebApplicationFactory`. |

---

## 🚀 Key Architectural Highlights

1. **Transient Kernel Lifecycle:** Prevents cross-tenant context bleeding and memory leakage by instantiating lightweight, scoped Semantic Kernel instances per execution turn.
2. **Framework Independence:** The core business domain (`AgentTemplate.Domain` and `AgentTemplate.Application`) contains **zero** references to Semantic Kernel or AI SDKs.
3. **Identity-Bound Security:** LLM plugin parameters never accept model-supplied `userId` or `tenantId`. All security claims are bound ambiently through `ICurrentUserContext`.
4. **Resilient AI Invocations:** Configured Polly HTTP pipelines with jittered exponential backoff, circuit breaking, and timeout budgets tailored for LLM inference latency.
5. **Full Observability:** End-to-end distributed tracing, standard GenAI token metrics (`gen_ai.usage.prompt_tokens`, `gen_ai.usage.completion_tokens`), and Zero-PII logging redaction.
6. **Real-time SSE Streaming:** Exposes Server-Sent Events (`/api/v1/agent/chat/stream`) emitting real-time agent thoughts, tool invocations, and token deltas.
