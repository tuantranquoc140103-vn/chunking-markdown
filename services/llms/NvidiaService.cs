


using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

public class NvidiaService : LlmChatCompletionBase
{
    public NvidiaService(JsonSerializerOptions jsonSerializerOptions, MarkdownService markdownService, ILlmConfigFactory llmConfigFactory, ILlmClientFactory llmClientFactory, IOptions<SystemPrompts> systemPrompts) 
    : base(jsonSerializerOptions, markdownService, llmConfigFactory, llmClientFactory, systemPrompts)
    {
    }

    public override JsonObject CreateRequestJsonChema<TModel>(List<ChatMessageRequest> messagesRequest, LlmModelConfig model) where TModel : class
    {
        JsonObject requestBinary = new JsonObject()
            {
                ["model"] = model.ModelName,
                ["messages"] = JsonSerializer.SerializeToNode(messagesRequest, _jsonSerializerOptions),
                ["temperature"] = model.Temperature,
                ["max_tokens"] = model.MaxTokens,
                ["stream"] = false,
                ["guided_json"] = CreatejsonChema<TModel>()
            };

        return requestBinary;
    }

    public override JsonObject CreateRequestChoice(List<ChatMessageRequest> messagesRequest,List<string> choices, LlmModelConfig model)
    {
        JsonObject requestBinary = new JsonObject()
                {
                    ["model"] = model.ModelName,
                    ["messages"] = JsonSerializer.SerializeToNode(messagesRequest, base._jsonSerializerOptions),
                    ["temperature"] = model.Temperature,
                    ["max_tokens"] = model.MaxTokens,
                    ["stream"] = false,
                    ["guided_choice"] = JsonSerializer.SerializeToNode(choices)
                };

        return requestBinary;
    }


}