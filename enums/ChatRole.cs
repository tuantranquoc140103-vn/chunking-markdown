using System.Runtime.Serialization;
using System.Text.Json.Serialization;

public enum ChatRole
{
    [JsonPropertyName("system")]
    [EnumMember(Value = "system")]
    System,
    [JsonPropertyName("user")]
    [EnumMember(Value = "user")]
    User,
    [JsonPropertyName("assistant")]
    [EnumMember(Value = "assistant")]
    Assistant
}