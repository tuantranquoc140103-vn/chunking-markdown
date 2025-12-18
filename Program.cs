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

Env.Load();


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Configuration.AddEnvironmentVariables();

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
        LlmOption llmOption = builder.Configuration.GetRequiredSection("LlmProvider:Vllm").Get<LlmOption>() ?? throw new ArgumentNullException("LlmProvider:Vllm is missing in appsettings.json");  
        var options = new OpenAIClientOptions { Endpoint = new Uri(llmOption.BaseUrl) };
        var client = new OpenAIClient(new ApiKeyCredential(llmOption.ApiKey ?? "no-api-key"), options);
        return client.GetChatClient(llmOption.ModelName);
    });
builder.Services.AddKeyedSingleton<ChatClient>(LlmProvider.Nvidia, (sp, key) =>
{
    LlmOption llmOption = builder.Configuration.GetRequiredSection("LlmProvider:Nvidia").Get<LlmOption>() ?? throw new ArgumentNullException("LlmProvider:Nvidia is missing in appsettings.json");  
    if(string.IsNullOrEmpty(llmOption.ApiKey)) throw new ArgumentNullException("LlmProvider:Nvidia ApiKey is missing in appsettings.json or env file");
    var client = new ChatClient(
        model: llmOption.ModelName,
        credential: new ApiKeyCredential(llmOption.ApiKey),
        options: new OpenAIClientOptions { Endpoint = new Uri(llmOption.BaseUrl) }
    );
    return client;
});
builder.Services.AddHttpClient<TokenCountService>(
    client =>
    {
        string url = builder.Configuration.GetRequiredSection("TokenCountService:BaseUrl").Value ?? throw new ArgumentNullException("TokenCountService:BaseUrl Key is missing in env file or appsettings.json");
        client.BaseAddress = new Uri(url);
        client.DefaultRequestHeaders.Add("Accept", "application/json");      
    }
);
builder.Services.AddScoped<MarkdownService>();

// Adapter
builder.Services.AddScoped<NvidiaAdapter>();
builder.Services.AddScoped<VllmAdapter>();

// Factory
builder.Services.AddSingleton<ILlmFactory, LlmFactory>();
builder.Services.AddSingleton<ILlmAdapterFactory, LlmAdapterFactory>();

builder.Services.AddScoped<NvidiaService>();
builder.Services.AddScoped<VllmService>();

var app = builder.Build();  



NvidiaService nvidiaService = app.Services.GetRequiredService<NvidiaService>();
// VllmService vllmService = app.Services.GetRequiredService<VllmService>();
var jsonObjectTest = nvidiaService._llmProviderAdapter.CreatejsonChema<Test>();
string jsonSchema = jsonObjectTest.ToJsonString();
// Console.WriteLine(jsonSchema);
string review = "Inception is a really well made film. I rate it four stars out of five.";
// string prompt = $@"Return the title and the rating based on the following movie review according to this JSON schema {jsonSchema}.

//                 Review: {review}";
string prompt = $@"Extract the movie title and rating from the following review.
Review movie: {review}";
List<ChatMessageRequest> messages = new List<ChatMessageRequest>()
{
    new ChatMessageRequest{Role = ChatRole.System, Content = "You are an intelligent AI assistant that helps extract user information into JSON."},
    new ChatMessageRequest{Role = ChatRole.User, Content = prompt}
};
Test res = nvidiaService.ChatWithStructuredJsonSchemaAsync<Test>(messages).Result;

Console.WriteLine(JsonSerializer.Serialize(res));