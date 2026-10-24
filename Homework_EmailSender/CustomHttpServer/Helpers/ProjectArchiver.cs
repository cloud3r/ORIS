using System.IO.Compression;

namespace CustomHttpServer.Helpers;

public static class ProjectArchiver
{
    private static readonly string[] ExcludedDirs = { "bin", "obj", ".git", ".vs", ".idea", "node_modules" };

    public static MemoryStream BuildProjectZip()
    {
        string projectRoot = FindProjectRoot();

        var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddDirectoryToZip(archive, projectRoot, projectRoot);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    private static void AddDirectoryToZip(ZipArchive archive, string currentDir, string rootDir)
    {
        foreach (string file in Directory.GetFiles(currentDir))
        {
            string relativePath = Path.GetRelativePath(rootDir, file);
            archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
        }

        foreach (string dir in Directory.GetDirectories(currentDir))
        {
            string dirName = Path.GetFileName(dir);

            if (ExcludedDirs.Contains(dirName, StringComparer.OrdinalIgnoreCase))
                continue;

            AddDirectoryToZip(archive, dir, rootDir);
        }
    }

    private static string FindProjectRoot()
    {
        string dir = Directory.GetCurrentDirectory();

        while (dir != null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0)
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName!;
        }

        throw new InvalidOperationException("Не удалось найти корень проекта (.csproj или .sln)");
    }
}