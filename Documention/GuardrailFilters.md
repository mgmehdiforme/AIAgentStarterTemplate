# Guardrail Filters & Safety Architecture

In production generative AI systems, uncontrolled autonomous execution can lead to infinite tool invocation loops, unauthorized actions, prompt injection, and PII leakage.

This starter template implements a **3-tier Semantic Kernel filter pipeline**:

```
[ Incoming User Prompt ]
          │
          ▼
┌─────────────────────────────────┐
│       PromptRenderFilter        │  ◄── PII Redaction, Input Scrubbing
└────────────────┬────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│       LoopCappingFilter         │  ◄── Prevents Infinite Tool Calling Loops
└────────────────┬────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│    ToolInvocationAuditFilter    │  ◄── Tool Authorization (RBAC) & Execution Auditing
└────────────────┬────────────────┘
                 │
                 ▼
      [ Execute Tool Function ]
```

---

## 1. Loop Capping Filter (`LoopCappingFilter`)

### The Problem
When an LLM model encounters ambiguous input or hallucinates missing parameters, it can enter an infinite tool-calling loop (e.g., repeatedly calling `SearchProducts` with minor variations), quickly exhausting token quotas and stalling server threads.

### Implementation
Implements `IAutoFunctionInvocationFilter`. Tracks the round-trip turn counter `context.RequestSequenceIndex`. If the turn index reaches or exceeds the configured limit (default: 5), it halts execution immediately:

```csharp
public async Task OnAutoFunctionInvocationAsync(
    AutoFunctionInvocationContext context, 
    Func<AutoFunctionInvocationContext, Task> next)
{
    if (context.RequestSequenceIndex >= _maxAllowedIterations)
    {
        context.Terminate = true; // Signal Semantic Kernel to halt auto tool-calling
        
        throw new AgentLoopExhaustedException(
            $"Agent execution halted: Exceeded maximum allowed tool loops ({_maxAllowedIterations}).",
            _maxAllowedIterations);
    }

    await next(context);
}
```

---

## 2. Tool Invocation Audit & RBAC Filter (`ToolInvocationAuditFilter`)

### The Problem
Autonomous agents should not be allowed to perform irreversible or sensitive actions (such as order cancellations, fund transfers, or records deletions) without explicit permission checks and thorough audit logs.

### Implementation
Implements `IFunctionInvocationFilter`. Intercepts function execution to:
1. Identify sensitive operations (e.g., functions matching `Cancel*` or `Delete*`).
2. Verify that the ambient `ICurrentUserContext` holds required permissions (e.g. `"Orders.Cancel"` or `"Admin"`).
3. Record duration and metadata for operational security audits.

```csharp
public async Task OnFunctionInvocationAsync(
    FunctionInvocationContext context, 
    Func<FunctionInvocationContext, Task> next)
{
    var functionName = context.Function.Name;
    var isSensitive = functionName.Contains("Cancel", StringComparison.OrdinalIgnoreCase);

    if (isSensitive && !_userContext.HasPermission("Orders.Cancel"))
    {
        throw new UnauthorizedOperationException(
            functionName,
            _userContext.UserId,
            $"User '{_userContext.UserId}' lacks required permissions to invoke '{functionName}'.");
    }

    var sw = Stopwatch.StartNew();
    try
    {
        await next(context);
    }
    finally
    {
        sw.Stop();
        _logger.LogInformation("Audit: Tool '{Function}' executed in {ElapsedMs}ms", functionName, sw.ElapsedMilliseconds);
    }
}
```

---

## 3. Prompt Render Filter (`PromptRenderFilter`)

### The Problem
Users might inadvertently paste sensitive customer information (e.g., payment card numbers) into chat prompts, which would otherwise be dispatched in plaintext to upstream external LLM APIs.

### Implementation
Implements `IPromptRenderFilter`. Operates immediately after prompt template variables are rendered, sanitizing sensitive regex patterns (such as credit card numbers) before network serialization:

```csharp
public static string SanitizePrompt(string prompt)
{
    if (string.IsNullOrEmpty(prompt)) return prompt;
    return CreditCardRegex().Replace(prompt, "[REDACTED_CC]");
}
```
