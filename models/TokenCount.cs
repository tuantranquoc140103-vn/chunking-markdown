using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;



public class CountRequest
{
    [Required]
    [JsonPropertyName("text")]
    public required string Text { get; set; }
    [JsonPropertyName("return_tokens")]
    public bool ReturnTokens { get; set; } = false;
}

public class CountResponse
{
    [JsonPropertyName("token_count")]
    public int TokenCount { get; set; }
    [JsonPropertyName("token_ids")]
    public List<int>? TokenIds { get; set; }
}

public class BatchCountRequest
{
    [Required, MinLength(1), MaxLength(2000)]
    [JsonPropertyName("texts")]
    public required List<string> Texts { get; set; }
    [JsonPropertyName("return_tokens")]
    public bool ReturnTokens { get; set; } = false;
}

public class BatchCountItem
{
    [JsonPropertyName("token_count")]
    public int TokenCount { get; set; }

    [JsonPropertyName("token_ids")]
    public List<int>? TokenIds { get; set; }
}

public class BatchCountResponse
{
    [JsonPropertyName("results")]
    public required List<BatchCountItem> Results { get; set; }
}