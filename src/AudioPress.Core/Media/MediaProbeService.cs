using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using AudioPress.Core.Tools;

namespace AudioPress.Core.Media;

public sealed class MediaProbeService
{
    private readonly FfmpegToolSet _tools;

    public MediaProbeService(FfmpegToolSet tools)
    {
        _tools = tools;
    }

    public async Task<MediaInfo> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var psi = new ProcessStartInfo
        {
            FileName = _tools.FfprobePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in new[]
                 {
                     "-v", "error",
                     "-show_format",
                     "-show_streams",
                     "-of", "json",
                     inputPath
                 })
        {
            psi.ArgumentList.Add(argument);
        }

        using var process = Process.Start(psi) ?? throw new MediaProbeException("无法启动 ffprobe。");
        using var stdoutBuffer = new MemoryStream();
        var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(stdoutBuffer, cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new MediaProbeException(string.IsNullOrWhiteSpace(stderr) ? "ffprobe 无法识别该文件。" : stderr.Trim());
        }

        return ParseProbeJson(stdoutBuffer.ToArray(), inputPath);
    }

    private static MediaInfo ParseProbeJson(ReadOnlyMemory<byte> json, string inputPath)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            TimeSpan? duration = null;
            string? title = null;
            string? artist = null;
            string? album = null;
            string? primaryCodec = null;

            if (root.TryGetProperty("format", out var format))
            {
                if (format.TryGetProperty("duration", out var durationElement)
                    && durationElement.ValueKind == JsonValueKind.String
                    && double.TryParse(durationElement.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                    && seconds > 0)
                {
                    duration = TimeSpan.FromSeconds(seconds);
                }

                if (format.TryGetProperty("tags", out var tags))
                {
                    title = GetPropertyIgnoreCase(tags, "title");
                    artist = GetPropertyIgnoreCase(tags, "artist");
                    album = GetPropertyIgnoreCase(tags, "album");
                }
            }

            if (root.TryGetProperty("streams", out var streams) && streams.ValueKind == JsonValueKind.Array)
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    if (GetPropertyIgnoreCase(stream, "codec_type") == "audio")
                    {
                        primaryCodec = GetPropertyIgnoreCase(stream, "codec_name");
                        break;
                    }
                }
            }

            var fileInfo = new FileInfo(inputPath);
            return new MediaInfo(duration, title, artist, album, primaryCodec, fileInfo.Exists ? fileInfo.Length : null);
        }
        catch (JsonException ex)
        {
            throw new MediaProbeException("ffprobe 返回的数据无法解析。", ex);
        }
    }

    private static string? GetPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.ToString();
            }
        }

        return null;
    }
}
