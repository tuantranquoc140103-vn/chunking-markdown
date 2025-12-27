using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Polly.Extensions.Http;
using Polly;

Env.Load();


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError() // Tự động bắt lỗi 5xx hoặc 408 (Timeout)
    .OrResult(msg => msg.StatusCode != System.Net.HttpStatusCode.OK) // Hoặc bất kỳ lỗi nào khác 200
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))); // Exponential backoff (2s, 4s, 8s)

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Configuration.AddEnvironmentVariables();

ChunkOption chunkOption = builder.Configuration.GetRequiredSection(ChunkOption.NameSection).Get<ChunkOption>() ?? throw new ArgumentNullException("ChunkOption is missing in appsettings.json");
LlmProviderOptions llmProviderOptions = builder.Configuration.GetRequiredSection(LlmProviderOptions.NameSection).Get<LlmProviderOptions>() ?? throw new ArgumentNullException("LlmProviderOptions is missing in appsettings.json");
SystemPrompts systemPrompt = builder.Configuration.GetRequiredSection(SystemPrompts.SectionName).Get<SystemPrompts>() ?? throw new ArgumentNullException("SystemPrompt is missing in appsettings.json");

builder.Services.AddSingleton<JsonSerializerOptions>( sp =>
{
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

    return options;
});

// Service
builder.Services.AddKeyedSingleton<ChatClient>(LlmProvider.Vllm, (sp, key) =>
    {
        
        var options = new OpenAIClientOptions { Endpoint = new Uri(llmProviderOptions.Vllm.BaseUrl) };
        var client = new OpenAIClient(new ApiKeyCredential(llmProviderOptions.Vllm.ApiKey ?? "no-api-key"), options);
        var models = llmProviderOptions.Vllm.Models;
        if(models.Count == 0)
        {
            // bắt buộc phải set vì chat client khong cho phép null cho thuộc tính model name
            return client.GetChatClient("no-model");   
        }
        
        return client.GetChatClient(llmProviderOptions.Vllm.Models[0]?.ModelName);
    });
builder.Services.AddKeyedSingleton<ChatClient>(LlmProvider.Nvidia, (sp, key) =>
{
    string? apiKey = llmProviderOptions.Nvidia.ApiKey;
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new ArgumentNullException(apiKey, "LlmProvider:Nvidia ApiKey is missing in appsettings.json or .env file");
        // throw new ArgumentNullException("LlmProvider:Nvidia ApiKey is missing in appsettings.json or .env file");
    }
    var models = llmProviderOptions.Nvidia.Models;
    if(models.Count == 0)
    {
        throw new ArgumentException("LlmProvider:Nvidia Models list is empty in appsettings.json");
    }
    var llmOption = models[0];
    return new ChatClient(
        model: llmOption.ModelName,
        credential: new ApiKeyCredential(apiKey),
        options: new OpenAIClientOptions { Endpoint = new Uri(llmProviderOptions.Nvidia.BaseUrl) }
    );
});
builder.Services.AddHttpClient<TokenCountService>(
    client =>
    {
        string url = builder.Configuration.GetRequiredSection("TokenCountService:BaseUrl").Value ?? throw new ArgumentNullException("TokenCountService:BaseUrl Key is missing in env file or appsettings.json");
        client.BaseAddress = new Uri(url);
        client.DefaultRequestHeaders.Add("Accept", "application/json");      
    }
).AddPolicyHandler(retryPolicy);

// Option
builder.Services.Configure<ChunkOption>(builder.Configuration.GetRequiredSection(ChunkOption.NameSection));
builder.Services.Configure<LlmProviderOptions>(builder.Configuration.GetRequiredSection(LlmProviderOptions.NameSection));
builder.Services.Configure<SystemPrompts>(builder.Configuration.GetRequiredSection(SystemPrompts.SectionName));

// Factory
builder.Services.AddSingleton<ILlmClientFactory, LlmClientFactory>();
builder.Services.AddSingleton<ILlmConfigFactory, LlmConfigFactory>();
builder.Services.AddSingleton<ILlmServiceFactory, LlmServiceFactory>();

// Service
builder.Services.AddKeyedSingleton<LlmChatCompletionBase, NvidiaService>(LlmProvider.Nvidia);
builder.Services.AddKeyedSingleton<LlmChatCompletionBase, VllmService>(LlmProvider.Vllm);
builder.Services.AddScoped<MarkdownService>();

var app = builder.Build();  

// string pathTableBefor = @"data/table_befor.md";
// string pathTableAfter = @"data\table_after.md";
// string pathPromptChoice = @"data\prompts\promptChoice.md";

// string dataTableBefor = File.ReadAllText(pathTableBefor);
// string dataTableAfter = File.ReadAllText(pathTableAfter);
// string templatePromptChoice = File.ReadAllText(pathPromptChoice);   

// ILlmServiceFactory llmServiceFactory = app.Services.GetRequiredService<ILlmServiceFactory>();

// LlmChatCompletionBase llmChoice = llmServiceFactory.GetLlmProviderChoice();
// LlmChatCompletionBase llmJsonSchema = llmServiceFactory.GetLlmProviderJsonSchema();

// string systemPromptChoice = @"You are a data processing assistant specializing in Markdown table structure analysis.";

// List<string> choices = new List<string>() { "yes", "no" };
// var stringChoices = string.Join(", ", choices);

// string finalPromptChoice = string.Format(templatePromptChoice, dataTableBefor, dataTableAfter);
// Console.WriteLine(finalPromptChoice);


// List<ChatMessageRequest> messages = new List<ChatMessageRequest>()
// {
//     new ChatMessageRequest{Role = ChatRole.System, Content = systemPromptChoice},
//     new ChatMessageRequest{Role = ChatRole.User, Content = finalPromptChoice}
// };


// string res = llmChoice.ChatWithStructuredChoiceAsync(messages, choices).Result;
// Console.WriteLine(res);


// var jsonObjectTest = llmJsonSchema.CreatejsonChema<Test>();

// string jsonSchema = jsonObjectTest.ToJsonString();
// // // Console.WriteLine(jsonSchema);
// string review = "Inception is a really well made film. I rate it four stars out of five.";
// string prompt = $@"Return the title and the rating based on the following movie review according to this JSON schema {jsonSchema}.

//                 Review: {review}";
// // string prompt = $@"Extract the movie title and rating from the following review.
// // Review movie: {review}";
// List<ChatMessageRequest> messages = new List<ChatMessageRequest>()
// {
//     new ChatMessageRequest{Role = ChatRole.System, Content = "You are an intelligent AI assistant that helps extract user information into JSON."},
//     new ChatMessageRequest{Role = ChatRole.User, Content = prompt}
// };
// Test res = llmJsonSchema.ChatWithStructuredJsonSchemaAsync<Test>(messages).Result;

// Console.WriteLine(JsonSerializer.Serialize(res));

MarkdownService markdownService = app.Services.GetRequiredService<MarkdownService>();

string pathData = @"data\example2.md";
string pathTestTable = @"data\test_table.md";
string pathOutputTable = @"data\outputTable.md";

string dataTestTable = File.ReadAllText(pathTestTable);
string sourceData = File.ReadAllText(pathData);



// var chunks = markdownService.CreateChunkDocument(dataTestTable).Result;
// var chunkTables = chunks.Where(c => c.Type == TypeChunk.Table).ToList();

TokenCountService tokenCountService = app.Services.GetRequiredService<TokenCountService>();

// var chunks = markdownService.CreateChunkTableInSection(dataTestTable).Result;
var chunks = markdownService.CreateChunkDocument(sourceData).Result;

// int numberToken = tokenCountService.CountAsync(new CountRequest {Text = chunks[0].Content, ReturnTokens = false}).Result.TokenCount;
// Console.WriteLine($"Token count: {numberToken}");
Console.WriteLine($"Total chunks: {chunks.Count}");

List<ChunkInfo> chunksTable = chunks.Where(c => c.Type == TypeChunk.Table).ToList();

string separator = $"{Environment.NewLine}{Environment.NewLine}# Table chunks{Environment.NewLine}{Environment.NewLine}";

var contentTable = chunks.Select(c =>
{
    string res = $@"Title: {c.Title}
Title hirarchy: {c.TittleHirarchy}
Type: {c.Type}
{c.Content}";
    return res;
});

File.WriteAllText(pathOutputTable, string.Join(separator, contentTable));
Console.WriteLine($"Total chunk tables: {chunksTable.Count}");