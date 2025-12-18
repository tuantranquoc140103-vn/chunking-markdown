

using System.Text.Json;
using System.Text.Json.Nodes;

public class NvidiaAdapter : LlmProviderAdapter
{
    public NvidiaAdapter(JsonSerializerOptions jsonSerializerOptions) : base(jsonSerializerOptions)
    {
    }

    public override JsonObject BuildBodyStructuredJsonSchema<T>()
    {

        var requestFormat = new JsonObject
        {
            ["guided_json"] = CreatejsonChema<T>()
        };

        return requestFormat;
        
    }

    public override JsonObject BuildChoice(List<string> choices)
    {
        throw new NotImplementedException();
    }
}