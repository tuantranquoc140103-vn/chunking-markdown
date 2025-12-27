using Microsoft.Extensions.Options;

public class LlmConfigFactory : ILlmConfigFactory
{
    private readonly ChunkOption _chunkOption;
    private readonly LlmProviderOptions _llmProviderOptions;

    public LlmConfigFactory(IOptions<ChunkOption> options, IOptions<LlmProviderOptions> llmProviderOptions)
    {
        _chunkOption = options.Value;
        _llmProviderOptions = llmProviderOptions.Value;
    }

    public (ProviderConfig, LlmModelConfig) GetProviderModelChoice()
    {
        string providerModel = _chunkOption.UseModelProviderForChoice;
        (string providerName, string modelName) = Unit.ParseModelProvider(providerModel);
        try
        {
            ProviderConfig provider = GetProvider((LlmProvider)Enum.Parse(typeof(LlmProvider), providerName, true));
            LlmModelConfig model = GetModel(provider, modelName);
            return (provider, model);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error get Provider {providerName} and Model {modelName}", ex);
        }
    }

    public (ProviderConfig, LlmModelConfig) GetProviderModelJsonSchema()
    {
        string providerModel = _chunkOption.UseModelProviderForJsonSchema;
        (string providerName, string modelName) = Unit.ParseModelProvider(providerModel);
        try
        {
            ProviderConfig provider = GetProvider((LlmProvider)Enum.Parse(typeof(LlmProvider), providerName, true));
            LlmModelConfig model = GetModel(provider, modelName);
            return (provider, model);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error get Provider {providerName} and Model {modelName}", ex);
        }
    }

    public ProviderConfig GetProvider(LlmProvider provider)
    {
        switch (provider)
        {
            case LlmProvider.Nvidia:
                return _llmProviderOptions.Nvidia;
            case LlmProvider.Vllm:
                return _llmProviderOptions.Vllm;
            default:
                throw new ArgumentException($"LlmProvider:{provider} is missing in appsettings.json");
        }
    }

    public LlmModelConfig GetModel(ProviderConfig provider, string modelName)
    {
        LlmModelConfig? modelConfig = provider.Models.FirstOrDefault(x => x.ModelName == modelName);

        if (modelConfig is null)
        {
            throw new ArgumentException($"Model:{modelName} is missing in appsettings.json");
        }
        return modelConfig;
    }
}