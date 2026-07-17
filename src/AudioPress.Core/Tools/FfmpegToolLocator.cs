namespace AudioPress.Core.Tools;

public static class FfmpegToolLocator
{
    public static FfmpegToolSet Resolve(string? baseDirectory = null)
    {
        var executableSuffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
        var ffmpegName = "ffmpeg" + executableSuffix;
        var ffprobeName = "ffprobe" + executableSuffix;

        var candidates = new List<string>();
        var environmentDirectory = Environment.GetEnvironmentVariable("AUDIOPRESS_FFMPEG_DIR");
        if (!string.IsNullOrWhiteSpace(environmentDirectory))
        {
            candidates.Add(environmentDirectory);
        }

        var root = baseDirectory ?? AppContext.BaseDirectory;
        candidates.Add(Path.Combine(root, "tools", "ffmpeg"));
        candidates.Add(Path.Combine(root, "ffmpeg"));
        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "tools", "ffmpeg"));

        foreach (var directory in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var ffmpegPath = Path.Combine(directory, ffmpegName);
            var ffprobePath = Path.Combine(directory, ffprobeName);
            if (File.Exists(ffmpegPath) && File.Exists(ffprobePath))
            {
                return new FfmpegToolSet(ffmpegPath, ffprobePath);
            }
        }

        return new FfmpegToolSet(ffmpegName, ffprobeName);
    }
}

