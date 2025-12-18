
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Microsoft.Extensions.Options;

public interface ILlmProviderAdapter
{
    JsonObject BuildBodyStructuredJsonSchema<T>() where T : class;
    JsonObject BuildChoice(List<string> choices);
}

public abstract class LlmProviderAdapter : ILlmProviderAdapter
{
    public readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly JsonSchemaExporterOptions _llmSchemaExporterOptions = new()
    {
      TreatNullObliviousAsNonNullable = true  
    };

    public LlmProviderAdapter(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }
    public JsonObject  CreatejsonChema<T>() where T : class
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
    public abstract JsonObject BuildChoice(List<string> choices);
    public abstract JsonObject BuildBodyStructuredJsonSchema<T>() where T : class;
}