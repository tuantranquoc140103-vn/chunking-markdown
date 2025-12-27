public class LlmModelConfig
{
    public string ModelName { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.5;
    public int MaxTokens { get; set; } = 8192;
}

public class ProviderConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; } = string.Empty;
    
    public List<LlmModelConfig> Models { get; set; } = new();
}

public class LlmProviderOptions
{
    public const string NameSection = "LlmProviders";
    public ProviderConfig Nvidia { get; set; } = new();
    public ProviderConfig Vllm { get; set; } = new();
}