# Quickstart Guide

This guide will walk you through setting up, configuring, and running the .NET 9 AI Agent Starter Template on your machine.

---

## 1. Prerequisites

- **.NET 9 SDK** (or later) installed:
  ```bash
  dotnet --version
  ```
- **Docker Desktop** / **Podman** (optional, recommended for Aspire AppHost container orchestration):
  - Required for running local Ollama, Redis, and Qdrant containers automatically.
- *(Alternative)* A running **Ollama** daemon locally or an **OpenAI** / **Azure OpenAI** API key.

---

## 2. Cloning & Restoring

```bash
git clone https://github.com/mgmehdiforme/AIAgentStarterTemplate.git
cd AIAgentStarterTemplate

# Restore all NuGet packages
dotnet restore AgentTemplate.sln
```

---

## 3. Configuration (`appsettings.json`)

The template supports dual AI providers configured via `appsettings.json`:

```json
{
  "AI": {
    "Provider": "Ollama",
    "FastModel": {
      "ModelId": "llama3.2",
      "Endpoint": "http://localhost:11434/v1",
      "ApiKey": "unused"
    },
    "ReasoningModel": {
      "ModelId": "llama3.2",
      "Endpoint": "http://localhost:11434/v1",
      "ApiKey": "unused"
    },
    "MaxAllowedToolLoops": 5,
    "RequestTimeoutSeconds": 120
  }
}
```

### Switching to Cloud OpenAI
Set `"Provider": "OpenAI"`, update `ApiKey` to your secret key, and omit `Endpoint` (or point to `https://api.openai.com/v1`).

### Switching to Azure OpenAI
Set `"Provider": "AzureOpenAI"`, update `Endpoint` to your Azure resource URI, `ApiKey` to your key, and `DeploymentName` to your Azure deployment.

---

## 4. Running the Application

### Option A: Via .NET Aspire (Full Container Orchestration)
Runs the Aspire dashboard, Redis cache, Qdrant vector database, Ollama container, and the Agent Web API simultaneously:
```bash
dotnet run --project src/Aspire/AgentTemplate.AppHost/AgentTemplate.AppHost.csproj
```
Open the Aspire dashboard link printed in the terminal (usually `http://localhost:18888`) to view real-time traces, metrics, and logs.

### Option B: Standalone Web API
If you have local Ollama running (`ollama serve`) and want to run just the API:
```bash
dotnet run --project src/Presentation/AgentTemplate.Api/AgentTemplate.Api.csproj
```
The API starts on `http://localhost:5000` (or configured port) and will automatically create and seed the SQLite database `agent_template.db`.

---

## 5. Exploring the API

### Interactive Documentation (Scalar UI)
Navigate to:
```
http://localhost:5000/scalar/v1
```
The modern Scalar API reference provides an interactive playground for invoking agent endpoints and inspecting schemas.

### Testing Synchronous Agent Chat
```bash
curl -X POST http://localhost:5000/api/v1/agent/chat \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-default" \
  -H "X-User-Id: user-demo" \
  -d '{"userMessage": "What products do you have available in stock?"}'
```

### Testing Server-Sent Events (SSE) Streaming
```bash
curl -N -X POST http://localhost:5000/api/v1/agent/chat/stream \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-default" \
  -H "X-User-Id: user-demo" \
  -d '{"userMessage": "Check my recent orders and summarize the total."}'
```
You will receive streamed SSE frames:
```
event: AgentThoughtStreamEvent
data: {"SessionId":"...","Thought":"Analyzing user request...","Timestamp":"..."}

event: AgentDeltaStreamEvent
data: {"SessionId":"...","Delta":"You have 1 recent order...","Timestamp":"..."}

event: AgentCompletedStreamEvent
data: {"SessionId":"...","FinalResponse":"...","TotalTokensUsed":35,"Timestamp":"..."}
```

---

## 6. Running Tests

Execute the automated test suites:
```bash
dotnet test AgentTemplate.sln
```
All unit tests and integration tests will execute and verify health checks, security isolation, and guardrails.
