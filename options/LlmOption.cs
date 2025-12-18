public class LlmOption
{
    public required string ModelName { get; set; }
    public string? ApiKey { get; set; }
    public required string BaseUrl { get; set; }

    // tham số điều khiển model
    public double Temperature { get; set; } = 0.6;
    public int MaxTokens { get; set; } = 4096;
    public bool Stream { get; set; } = false;
}