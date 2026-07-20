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

    private static readonly HashSet<string> SupportedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".mkv",
        ".mov",
        ".avi",
        ".wmv",
        ".webm",
        ".flv",
        ".f4v",
        ".m4v",
        ".ts",
        ".trp",
        ".tp",
        ".mts",
        ".m2ts",
        ".3gp",
        ".3g2",
        ".mpeg",
        ".mpg",
        ".vob",
        ".ogv",
        ".asf",
        ".rm",
        ".rmvb",
        ".mxf",
        ".dv",
        ".divx",
        ".mod",
        ".tod",
        ".dat",
        ".nut",
        ".y4m",
        ".ivf",
        ".264",
        ".h264",
        ".265",
        ".h265",
        ".hevc",
        ".av1"
    };

    public static bool IsLikelyAudioFile(string path)
        => File.Exists(path) && SupportedExtensions.Contains(Path.GetExtension(path));

    public static bool IsLikelyMediaFile(string path, bool includeVideo)
        => File.Exists(path)
           && (SupportedExtensions.Contains(Path.GetExtension(path))
               || includeVideo && SupportedVideoExtensions.Contains(Path.GetExtension(path)));

    public static IReadOnlyList<string> ScanFiles(IEnumerable<string> paths, bool recursive, bool includeVideo = false)
    {
        var files = new List<string>();

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                if (IsLikelyMediaFile(path, includeVideo))
                {
                    files.Add(Path.GetFullPath(path));
                }

                continue;
            }

            if (Directory.Exists(path))
            {
                files.AddRange(ScanDirectory(path, recursive, includeVideo));
            }
        }

        return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<string> ScanDirectory(string directory, bool recursive, bool includeVideo)
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
                if (IsLikelyMediaFile(candidate, includeVideo))
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
