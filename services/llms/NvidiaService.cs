


using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

public class NvidiaService : LlmChatCompletionBase<NvidiaAdapter>
{
    private ChatClient _client;
    private readonly LlmOption _llmOption;
    // private ILlmFactory _llmFactory;s
    public NvidiaService(ILlmAdapterFactory llmAdapterFactory, ILlmFactory llmFactory)
                : base(llmAdapterFactory)
    {
        (_client, _llmOption) = llmFactory.GetClient(LlmProvider.Nvidia);
        // PrintLlmOption(_llmOption);
    }

    public override Task<ChatCompletionResponse> ChatCompletionAsync(List<ChatMessageRequest> request)
    {
        throw new NotImplementedException();
    }

    public override Task<ChatCompletionResponse> ChatWithStructuredChoiceAsync(List<ChatMessageRequest> request, List<string> choices)
    {
        
        throw new NotImplementedException();
    }

    public override async Task<TModel> ChatWithStructuredJsonSchemaAsync<TModel>(List<ChatMessageRequest> messagesRequest)
    {

        try
        {
            
            JsonObject requestBinary = new JsonObject()
            {
                ["model"] = _llmOption.ModelName,
                ["messages"] = JsonSerializer.SerializeToNode(messagesRequest, _llmProviderAdapter._jsonSerializerOptions),
                ["temperature"] = _llmOption.Temperature,
                ["max_tokens"] = _llmOption.MaxTokens,
                ["stream"] = false,
                ["guided_json"] = _llmProviderAdapter.CreatejsonChema<TModel>()
            };
            // MergeFields(requestBinary, _llmProviderAdapter.CreatejsonChema<TModel>());
            string requestString = JsonSerializer.Serialize(requestBinary);
            // Console.WriteLine($"Request: {requestString}");
            BinaryData data = new BinaryData(requestString);
            BinaryContent content = BinaryContent.Create(data);

            var res = await _client.CompleteChatAsync(content);
            // Console.WriteLine($"Response status code: {res.GetRawResponse().Status}");
            PipelineResponse response = res.GetRawResponse();
            // Console.WriteLine($"Resonse: {response.Content}");
            string responseJson = response.Content.ToString();

            JsonObject resObj = JsonSerializer.Deserialize<JsonObject>(responseJson)!;

            string rawContent = resObj["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "";

            if (string.IsNullOrEmpty(rawContent))
            {
                throw new InvalidOperationException("API response content was empty or could not be deserialized.");
            }

            string cleanText = rawContent.Replace("<|return|>", "").Trim();

            TModel tModelResult = JsonSerializer.Deserialize<TModel>(cleanText)!;
            
            return tModelResult;
        }
        catch(ClientResultException ex)
        {
            
            throw new InvalidOperationException($"Error Calling Nvidia API. StatusCode: {ex.Status}", ex);
        }
        catch(JsonException ex)
        {
            throw new InvalidOperationException("Model returned invalid JSON. Check add attribute JsonPropertyName", ex);
        }
        catch(Exception ex)
        {
            throw new InvalidOperationException("Error ChatWithStructuredJsonSchema: ", ex);
        }
    }

    private void PrintLlmOption(LlmOption option)
    {
        Console.WriteLine("Nvidia LlmOption:");
        Console.WriteLine(JsonSerializer.Serialize(option));
    }
}