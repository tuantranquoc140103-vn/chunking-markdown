

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Sprache;

/// <summary>
/// Factory này dùng để lấy chatClient phù hợp với provider được config trong chunk option.
/// Nó trả về ChatClient ứng với provider và ProviderConfig của nó trong appsettings.json
/// </summary>
public class LlmClientFactory : ILlmClientFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LlmProviderOptions _llmProviderOptions;
    private readonly ChunkOption _chunkOption;

    public LlmClientFactory(IServiceProvider serviceProvider, IOptions<LlmProviderOptions> options,
        IOptions<ChunkOption> chunkOption)
    {
        _chunkOption = chunkOption.Value;
        _serviceProvider = serviceProvider;
        _llmProviderOptions = options.Value ?? throw new ArgumentNullException($"{LlmProviderOptions.NameSection} is missing in appsettings.json");
    }

    public ChatClient GetChatClientChoice()
    {
        (string providerName, string _) = Unit.ParseModelProvider(_chunkOption.UseModelProviderForChoice);
        if(Enum.TryParse(providerName, out LlmProvider provider))
        {
            return _serviceProvider.GetRequiredKeyedService<ChatClient>(provider) ?? throw new ArgumentNullException($"LlmProvider:{provider} is missing in appsettings.json");
        }

        throw new ArgumentNullException($"LlmProvider:{providerName} is missing in appsettings.json");
    }

    public ChatClient GetChatClientJsonSchema()
    {
        (string providerName, string _) = Unit.ParseModelProvider(_chunkOption.UseModelProviderForJsonSchema);
        if(Enum.TryParse(providerName, out LlmProvider provider))
        {
            return _serviceProvider.GetRequiredKeyedService<ChatClient>(provider) ?? throw new ArgumentNullException($"LlmProvider:{provider} is missing in appsettings.json");
        }

        throw new ArgumentNullException($"LlmProvider:{providerName} is missing in appsettings.json");
    }

    public (ChatClient, ProviderConfig) GetClient(LlmProvider llmProvider)
    {
        ProviderConfig? option = null;
        switch (llmProvider)
        {
            case LlmProvider.Vllm:
                option = _llmProviderOptions.Vllm;
                break;
            case LlmProvider.Nvidia:
                option = _llmProviderOptions.Nvidia;
                break;
        }
        var client = _serviceProvider.GetRequiredKeyedService<ChatClient>(llmProvider);
        if(client is null) 
        throw new ArgumentNullException($"LlmProvider:{llmProvider} is missing in appsettings.json");
        
        if(option is null) 
        throw new ArgumentNullException($"LlmProvider:{llmProvider} is missing in appsettings.json");
        return (client, option);
    }
}