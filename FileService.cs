using System.IO;

public class FileService
{
    private readonly string _baseDirectory;

    public FileService(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    public async Task<string> ReadFile(string relativePath)
    {
        if(string.IsNullOrEmpty(relativePath))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(relativePath));
        }
        string fullPath = Path.Combine(_baseDirectory, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found", fullPath);
        }

        return await File.ReadAllTextAsync(fullPath);
    }

    public void WriteFile(string relativePath, string content)
    {
        throw new NotImplementedException();
    }
}