namespace AgentTemplate.Infrastructure.Configuration;

public enum AiProviderType
{
    Ollama,
    OpenAI,
    AzureOpenAI
}

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public AiProviderType Provider { get; set; } = AiProviderType.Ollama;
    public ModelOptions FastModel { get; set; } = new()
    {
        ModelId = "llama3.2",
        Endpoint = "http://localhost:11434/v1"
    };
    public ModelOptions ReasoningModel { get; set; } = new()
    {
        ModelId = "llama3.2",
        Endpoint = "http://localhost:11434/v1"
    };
    public int MaxAllowedToolLoops { get; set; } = 5;
    public int RequestTimeoutSeconds { get; set; } = 120;
}

public sealed class ModelOptions
{
    public string ModelId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = "unused";
    public string DeploymentName { get; set; } = string.Empty;
}
