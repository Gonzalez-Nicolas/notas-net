namespace NotasNet.Api.Application;

public static class PromptLoader
{
    public static string Load(string fileName)
    {
        var assembly = typeof(PromptLoader).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName, StringComparison.Ordinal));
        using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
        return reader.ReadToEnd();
    }
}
