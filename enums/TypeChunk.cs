using System.ComponentModel;

public enum TypeChunk
{
    [Description("Paragraph")]
    Paragraph = 1,
    [Description("Header")]
    Header = 2,
    [Description("Table")]
    Table = 3,
    [Description("All content")]
    AllContent = 4
}