
public class ChunkOption
{
    public const string NameSection = "ChunkOption";
    public int MaxTokensPerChunk { get; set; } = 8192;
    public int MaxDeepHeader { get; set; } = 5;
    public required string UseModelProviderForChoice { get; set; }
    public required string UseModelProviderForJsonSchema { get; set; }
    public required string UseModelProviderForGenQA { get; set; }
    
}