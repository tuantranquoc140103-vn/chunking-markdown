

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

public class VllmAdapter : LlmProviderAdapter
{
    public VllmAdapter(JsonSerializerOptions jsonSerializerOptions) : base(jsonSerializerOptions)
    {
    }

    public override JsonObject BuildBodyStructuredJsonSchema<T>()
    {
        // var requestFormat = new
        // {
        //   type = "Json_schema",
        //   json_schema = new
        //   {
        //       name = typeof(T).Name,
        //       schema = CreatejsonChema<T>()
        //   }
        // };

        var requestFormat = new JsonObject
        {
          ["type"] = "Json_schema",
          ["json_schema"] = new JsonObject
          {
              ["name"] = typeof(T).Name,
              ["schema"] = CreatejsonChema<T>()
          }
        };
        
        return requestFormat;
    }

    public override JsonObject BuildChoice(List<string> choices)
    {
        throw new NotImplementedException();
    }
}