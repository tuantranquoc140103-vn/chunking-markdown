

using Microsoft.Extensions.DependencyInjection;


public class LlmAdapterFactory : ILlmAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;

    public LlmAdapterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    T ILlmAdapterFactory.Create<T>()
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}