
using OpenAI.Chat;

public interface ILlmFactory
{
    (ChatClient, LlmOption) GetClient(LlmProvider provider);
}