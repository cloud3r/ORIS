namespace CustomHttpServer.Framework.Helpers;

public static class UriHelper
{
    public static string? ResolveSafePath(
        string requestedPath,
        string staticDirectory)
    {
        try
        {
            if (requestedPath.Contains('\0'))
                return null;

            string rootPath = Path.GetFullPath(staticDirectory);

            string relativePath = Uri.UnescapeDataString(requestedPath);

            relativePath = relativePath.Replace(
                '/',
                Path.DirectorySeparatorChar
            );

            relativePath = relativePath.TrimStart(
                Path.DirectorySeparatorChar
            );

            string fullPath = Path.GetFullPath(
                Path.Combine(rootPath, relativePath)
            );

            string rootPrefix =
                Path.TrimEndingDirectorySeparator(rootPath)
                + Path.DirectorySeparatorChar;

            StringComparison comparison =
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

            if (!fullPath.StartsWith(rootPrefix, comparison))
                return null;

            return fullPath;
        }
        catch
        {
            return null;
        }
    }
}