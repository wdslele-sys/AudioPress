using System.IO;

namespace AudioPress.Services;

public static class AudioFileScanner
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".wav",
        ".flac",
        ".m4a",
        ".aac",
        ".ogg",
        ".opus",
        ".wma",
        ".aiff",
        ".aif",
        ".ac3",
        ".mka",
        ".ape",
        ".alac"
    };

    public static bool IsLikelyAudioFile(string path)
        => File.Exists(path) && SupportedExtensions.Contains(Path.GetExtension(path));

    public static IReadOnlyList<string> ScanFiles(IEnumerable<string> paths, bool recursive)
    {
        var files = new List<string>();

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                if (IsLikelyAudioFile(path))
                {
                    files.Add(Path.GetFullPath(path));
                }

                continue;
            }

            if (Directory.Exists(path))
            {
                files.AddRange(ScanDirectory(path, recursive));
            }
        }

        return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<string> ScanDirectory(string directory, bool recursive)
    {
        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            IEnumerable<string> files;

            try
            {
                files = Directory.EnumerateFiles(current);
            }
            catch
            {
                continue;
            }

            foreach (var candidate in files)
            {
                if (IsLikelyAudioFile(candidate))
                {
                    yield return Path.GetFullPath(candidate);
                }
            }

            if (!recursive)
            {
                continue;
            }

            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateDirectories(current);
            }
            catch
            {
                continue;
            }

            foreach (var child in children)
            {
                pending.Push(child);
            }
        }
    }
}
