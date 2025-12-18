

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

public class LlmFactory : ILlmFactory
{
    private readonly IServiceProvider _serviceProvider;

    public LlmFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public (ChatClient, LlmOption) GetClient(LlmProvider llmProvider)
    {
        LlmOption option = _serviceProvider.GetService<IConfiguration>()?.GetRequiredSection($"LlmProvider:{llmProvider}").Get<LlmOption>() ?? throw new ArgumentNullException($"LlmProvider:{llmProvider} is missing in appsettings.json");
        var client = _serviceProvider.GetRequiredKeyedService<ChatClient>(llmProvider);
        if(client is null) throw new ArgumentNullException($"LlmProvider:{llmProvider} is missing in appsettings.json");
        return (client, option);
    }
}