public interface ILlmAdapterFactory
{
    T Create<T>() where T : class, ILlmProviderAdapter; 
}