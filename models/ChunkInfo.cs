public class SpanChunk
{
    public int Start { get; set; }
    public int End { get; set; }
    public required TypeChunk Type { get; set; }
}

public class ChunkInfo
{
    public TypeChunk Type { get; set; }
    public int TokensCount { get; set; }
    public string? Title { get; set; } = string.Empty;
    public string? TittleHirarchy { get; set; } = string.Empty;
    public required string Content { get; set; }
}