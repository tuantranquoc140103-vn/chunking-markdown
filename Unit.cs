

public class Unit
{
    public static (string, string) ParseModelProvider(string modelProvider)
    {
        string[] parse = modelProvider.Split("__");
        string provierName = parse[0];
        string model = parse[1];
        return (provierName, model);
    } 
}