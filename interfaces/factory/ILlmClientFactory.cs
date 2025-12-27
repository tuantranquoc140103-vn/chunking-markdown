
using OpenAI.Chat;

public interface ILlmClientFactory
{
    (ChatClient, ProviderConfig) GetClient(LlmProvider provider);
    ChatClient GetChatClientChoice();
    ChatClient GetChatClientJsonSchema();
}