
public class Prompt
{
    public string SystemPrompt {get; set; } = string.Empty;
    public string PathPrompt {get; set; } = string.Empty;
}

public class SystemPrompts
{
    public const string SectionName = "SystemPrompts";
    public Prompt Choice { get; set; } = new Prompt();
    public Prompt GenQA { get; set; } = new Prompt();
}