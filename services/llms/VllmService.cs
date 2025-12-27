


using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

public class VllmService : LlmChatCompletionBase
{
    public VllmService(JsonSerializerOptions jsonSerializerOptions, MarkdownService markdownService, ILlmConfigFactory llmConfigFactory, ILlmClientFactory llmClientFactory, IOptions<SystemPrompts> systemPrompts) : base(jsonSerializerOptions, markdownService, llmConfigFactory, llmClientFactory, systemPrompts)
    {
    }

    public override JsonObject CreateRequestChoice(List<ChatMessageRequest> messagesRequest, List<string> choices, LlmModelConfig model)
    {
        throw new NotImplementedException();
    }

    public override JsonObject CreateRequestJsonChema<TModel>(List<ChatMessageRequest> messagesRequest, LlmModelConfig model)
    {
        throw new NotImplementedException();
    }
}