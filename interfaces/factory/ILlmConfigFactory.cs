public interface ILlmConfigFactory
{
    (ProviderConfig, LlmModelConfig) GetProviderModelChoice();
    (ProviderConfig, LlmModelConfig) GetProviderModelJsonSchema();
}