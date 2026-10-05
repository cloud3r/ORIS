namespace CustomHttpServer.Helpers;

public static class UriHelper
{
    public static string? ResolveSafePath(string urlPath, string rootDirectory)
    {
        var relativePath = urlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.GetFullPath(Path.Combine(rootDirectory, relativePath));
        
        var rootAbsolutePath = Path.GetFullPath(rootDirectory);
        if (!absolutePath.StartsWith(rootAbsolutePath, StringComparison.OrdinalIgnoreCase))
            return null;

        return absolutePath;
    }
}