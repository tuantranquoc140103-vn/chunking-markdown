

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sprache;


/// <summary>
/// Factory này dùng trong markdown service.
/// nó tự đông lấy service provider dự vào config chunk option để xác định nên dùng provider, model nào cho task nào
/// </summary>
public class LlmServiceFactory : ILlmServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ChunkOption _chunkOption;

    public LlmServiceFactory(IServiceProvider serviceProvider, IOptions<ChunkOption> options)
    {
        _serviceProvider = serviceProvider;
        _chunkOption = options.Value ?? throw new ArgumentNullException($"{ChunkOption.NameSection} is missing in appsettings.json");
    }

    public LlmChatCompletionBase GetLlmProviderChatQA()
    {
        string modelProviderChatQA = _chunkOption.UseModelProviderForGenQA;
        (string provierName, string model) = Unit.ParseModelProvider(modelProviderChatQA);
        if(Enum.TryParse(provierName, out LlmProvider provider))
        {
            return _serviceProvider.GetRequiredKeyedService<LlmChatCompletionBase>(provider) ?? throw new ArgumentNullException($"LlmProvider:{provider} is missing in appsettings.json");
        }
        else{
            throw new ArgumentNullException($"LlmProvider:{provierName} is missing in appsettings.json");
        }
    }

    public LlmChatCompletionBase GetLlmProviderChoice()
    {
        string modelProviderChoice = _chunkOption.UseModelProviderForChoice;
        (string provierName, string model) = Unit.ParseModelProvider(modelProviderChoice);
        if(Enum.TryParse(provierName, out LlmProvider provider))
        {
            return _serviceProvider.GetRequiredKeyedService<LlmChatCompletionBase>(provider) ?? throw new ArgumentNullException($"LlmProvider:{provider} is missing in appsettings.json");
        }
        else{
            throw new ArgumentNullException($"LlmProvider:{provierName} is missing in appsettings.json");
        }
    }

    public LlmChatCompletionBase GetLlmProviderJsonSchema()
    {
        string modelProviderJsonSchema = _chunkOption.UseModelProviderForJsonSchema;
        (string provierName, string model) = Unit.ParseModelProvider(modelProviderJsonSchema);
        if(Enum.TryParse(provierName, out LlmProvider provider))
        {
            return _serviceProvider.GetRequiredKeyedService<LlmChatCompletionBase>(provider) ?? throw new ArgumentNullException($"LlmProvider:{provider} is missing in appsettings.json");
        }
        else{
            throw new ArgumentNullException($"LlmProvider:{provierName} is missing in appsettings.json");
        }
    }

    
}