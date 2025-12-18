
using System.Text.Json.Serialization;



public class ChatMessageRequest
{
    [JsonPropertyName("role")]
    public ChatRole Role { get; set; }
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}
