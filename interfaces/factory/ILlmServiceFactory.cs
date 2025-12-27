

public interface ILlmServiceFactory
{
    LlmChatCompletionBase GetLlmProviderChatQA();
    LlmChatCompletionBase GetLlmProviderChoice();
    LlmChatCompletionBase GetLlmProviderJsonSchema();
}