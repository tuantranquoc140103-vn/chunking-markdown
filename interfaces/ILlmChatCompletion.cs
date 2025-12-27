
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.RegularExpressions;
using Markdig.Syntax;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using SharpToken;
using Sprache;

public interface ILlmChatCompletion
{
    // Task<ChatCompletionResponse> ChatCompletionAsync(List<ChatMessageRequest> request);
    Task<string> ChatWithStructuredChoiceAsync(List<ChatMessageRequest> request, List<string> choices);
    Task<TModel> ChatWithStructuredJsonSchemaAsync<TModel>(List<ChatMessageRequest> messagesRequest) where TModel : class;
}

public abstract class LlmChatCompletionBase : ILlmChatCompletion
{
    protected readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly JsonSchemaExporterOptions _llmSchemaExporterOptions = new()
    {
      TreatNullObliviousAsNonNullable = true  
    };

    private MarkdownService _markdownService;
    private readonly ILlmConfigFactory _llmConfigFactory;
    private readonly ILlmClientFactory _llmClientFactory;
    private SystemPrompts _systemPrompts;
    protected LlmChatCompletionBase(JsonSerializerOptions jsonSerializerOptions,
                            MarkdownService markdownService, ILlmConfigFactory llmConfigFactory,
                            ILlmClientFactory llmClientFactory, IOptions<SystemPrompts> systemPrompts)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
        _markdownService = markdownService;
        _llmConfigFactory = llmConfigFactory;
        _llmClientFactory = llmClientFactory;
        _systemPrompts = systemPrompts.Value;
    }

    public abstract JsonObject CreateRequestJsonChema<TModel>(List<ChatMessageRequest> messagesRequest, LlmModelConfig model) where TModel : class;
    public abstract JsonObject CreateRequestChoice(List<ChatMessageRequest> messagesRequest, List<string> choices, LlmModelConfig model);

    public virtual string ProcessResponse(PipelineResponse response)
    {
        // Console.WriteLine($"Resonse: {response.Content}");
        string responseJson = response.Content.ToString();

        JsonObject resObj = JsonSerializer.Deserialize<JsonObject>(responseJson)!;

        string rawContent = resObj["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "";

        if (string.IsNullOrEmpty(rawContent))
        {
            throw new InvalidOperationException("API response content was empty or could not be deserialized.");
        }

        string cleanText = ClearText(rawContent);

        return cleanText;
    }

    public async Task<string> ChatWithStructuredChoiceAsync(List<ChatMessageRequest> request, List<string> choices)
    {
        
        try
        {
            (var _, LlmModelConfig modelConfig) = _llmConfigFactory.GetProviderModelChoice();
            JsonObject requestJson = CreateRequestChoice(request, choices, modelConfig);
            string requestString = JsonSerializer.Serialize(requestJson);
            BinaryData data = new BinaryData(requestString);
            BinaryContent content = BinaryContent.Create(data);

            var _client = _llmClientFactory.GetChatClientChoice();
            var res = await _client.CompleteChatAsync(content);
            PipelineResponse response = res.GetRawResponse();

            string stringChoice = ProcessResponse(response);
            stringChoice = ParseChoice(stringChoice, choices);
            if (string.IsNullOrEmpty(stringChoice))
            {
                throw new InvalidOperationException("API response content was empty for choice.");
            }
            return stringChoice;
        }
        catch(ClientResultException ex)
        {
            if(ex.Status == 400)
            {
                throw new ArgumentException($"Error Calling Nvidia API. StatusCode: {ex.Status}. Message: Model do not support choices", ex);
            }
            throw new InvalidOperationException($"Error Calling Nvidia API. StatusCode: {ex.Status}. Message: {ex.Message}", ex);
        }
        catch(Exception ex)
        {
            throw new InvalidOperationException("Error ChatWithStructuredChoiceAsync: ", ex);
        }
    }

    public async Task<TModel> ChatWithStructuredJsonSchemaAsync<TModel>(List<ChatMessageRequest> messagesRequest) where TModel : class
    {
        string cleanText = string.Empty;
        try
        {
            (var _, LlmModelConfig modelConfig) = _llmConfigFactory.GetProviderModelJsonSchema();
            JsonObject requestJson = CreateRequestJsonChema<TModel>(messagesRequest, modelConfig);
            string requestString = JsonSerializer.Serialize(requestJson);
            // Console.WriteLine($"Request: {requestString}");
            BinaryData data = new BinaryData(requestString);
            BinaryContent content = BinaryContent.Create(data);

            var client = _llmClientFactory.GetChatClientJsonSchema();
            if(client is null)
            {
                throw new ArgumentNullException("Client is null. Please implement GetLlmClient()");
            }
            var res = await client.CompleteChatAsync(content);
            // Console.WriteLine($"Response status code: {res.GetRawResponse().Status}");
            PipelineResponse response = res.GetRawResponse();

            cleanText = ProcessResponse(response);

            TModel tModelResult = JsonSerializer.Deserialize<TModel>(cleanText)!;
            
            return tModelResult;
        }
        catch(ClientResultException ex)
        {
            
            throw new InvalidOperationException($"Error Calling Nvidia API. StatusCode: {ex.Status}", ex);
        }
        catch(JsonException ex)
        {
            Console.Error.WriteLine($"Model returned invalid JSON. Check add attribute JsonPropertyName: {ex.Message}");
            Console.WriteLine($"Response: {cleanText}");
            Console.WriteLine("Trying process response by markdig...");

            List<Block> allBlock = _markdownService.GetAllBlock(cleanText);
            FencedCodeBlock? fencedCodeBlock = null;
            foreach (var block in allBlock)
            {
                Console.WriteLine(block.GetType().Name);
                if (block is FencedCodeBlock fenced)
                {
                    fencedCodeBlock = fenced;
                    break;
                }
            }
            if(fencedCodeBlock is null)
            {
                throw new InvalidOperationException("Model returned invalid JSON. Check add attribute JsonPropertyName", ex);
            }
            string language = fencedCodeBlock.Info ?? ""; 
        
            if (language == "json")
            {
                // Đây chính là khối bạn đang tìm
                // Lấy nội dung bên trong:
                cleanText = string.Join("\n", fencedCodeBlock.Lines);
                Console.WriteLine($"Found JSON block: {cleanText}");
            }
            try
            {
                TModel tModelResult = JsonSerializer.Deserialize<TModel>(cleanText)!;
                // Console.WriteLine($"Model result: {JsonSerializer.Serialize(tModelResult)}");
                return tModelResult;
            }
            catch(Exception e)
            {
                throw new InvalidOperationException("Try process response by markdig failed", e);
            }
        }
        catch(Exception ex)
        {
            throw new InvalidOperationException("Error ChatWithStructuredJsonSchema: ", ex);
        }
    }

    public string ClearText(string text)
    {
        return text.Replace("<|return|>", "").Trim();
    }

    public JsonObject CreatejsonChema<T>() where T : class
    {
        var result = JsonSchemaExporter.GetJsonSchemaAsNode(
            _jsonSerializerOptions,
            typeof(T),
            _llmSchemaExporterOptions
        );

        if(result is null)
        {
            throw new ArgumentNullException($"{typeof(T).Name} null. Could not create json schema");
        }

        return result.AsObject();
    }

    protected string ParseChoice(string rawResponse, List<string> validChoices)
    {
        if (string.IsNullOrWhiteSpace(rawResponse)) return string.Empty;

        // Bước 1: Nếu model trả về chính xác 1 từ trong list (Case xịn)
        string cleanResponse = rawResponse.Trim();
        var exactMatch = validChoices.FirstOrDefault(c => 
            string.Equals(c, cleanResponse, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null) return exactMatch;

        // Bước 2: Nếu model "nhiều lời", tìm từ khóa xuất hiện cuối cùng trong văn bản
        // Thường đáp án sẽ nằm ở cuối câu: "Thus, the answer is Good."
        string bestMatch = string.Empty;
        int lastIndex = -1;

        foreach (var choice in validChoices)
        {
            // Dùng Regex \b để tránh bắt nhầm (ví dụ "Good" trong "Goodbye")
            var match = Regex.Match(rawResponse, $@"\b{Regex.Escape(choice)}\b", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
            
            if (match.Success && match.Index > lastIndex)
            {
                lastIndex = match.Index;
                bestMatch = choice;
            }
        }

        return !string.IsNullOrEmpty(bestMatch) ? bestMatch : cleanResponse;
    }

    public List<ChatMessageRequest> CreateChatMessageChoice(string table1, string table2)
    {
        string path = _systemPrompts.Choice.PathPrompt;
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found for prompt choice: ", path);
        }
        string templatePromptChoice = File.ReadAllText(path);
        string finalPrompt = string.Format(templatePromptChoice, table1, table2);
        List<ChatMessageRequest> result = new List<ChatMessageRequest>()
        {
            new ChatMessageRequest{Role = ChatRole.System, Content = _systemPrompts.Choice.SystemPrompt},
            new ChatMessageRequest{Role = ChatRole.User, Content = finalPrompt}
        };
        return result;
        
    }
}

