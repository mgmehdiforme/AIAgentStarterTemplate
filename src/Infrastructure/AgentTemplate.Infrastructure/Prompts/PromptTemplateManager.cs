using AgentTemplate.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgentTemplate.Infrastructure.Prompts;

public sealed class PromptTemplateManager : IPromptTemplateManager
{
    private readonly string _promptsDirectory;
    private readonly ILogger<PromptTemplateManager> _logger;

    public PromptTemplateManager(IHostEnvironment environment, ILogger<PromptTemplateManager> logger)
    {
        _logger = logger;
        _promptsDirectory = Path.Combine(environment.ContentRootPath, "prompts");
    }

    public async Task<string> GetPromptAsync(string promptName, CancellationToken cancellationToken = default)
    {
        // Check for YAML format first, then skprompt.txt, then prompt.md
        var yamlPath = Path.Combine(_promptsDirectory, promptName, "prompt.yaml");
        if (File.Exists(yamlPath))
        {
            return await File.ReadAllTextAsync(yamlPath, cancellationToken);
        }

        var skPromptPath = Path.Combine(_promptsDirectory, promptName, "skprompt.txt");
        if (File.Exists(skPromptPath))
        {
            return await File.ReadAllTextAsync(skPromptPath, cancellationToken);
        }

        var mdPath = Path.Combine(_promptsDirectory, promptName, "prompt.md");
        if (File.Exists(mdPath))
        {
            return await File.ReadAllTextAsync(mdPath, cancellationToken);
        }

        _logger.LogWarning("Prompt template '{PromptName}' not found in '{Directory}'. Returning fallback persona.", promptName, _promptsDirectory);
        return "You are an intelligent, secure, and helpful AI assistant for Order & Product Operations. Use available tools accurately.";
    }
}
