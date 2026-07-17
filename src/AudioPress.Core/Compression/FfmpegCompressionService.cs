using System.Diagnostics;
using System.Globalization;
using AudioPress.Core.Tools;

namespace AudioPress.Core.Compression;

public sealed class FfmpegCompressionService
{
    private readonly FfmpegToolSet _tools;

    public FfmpegCompressionService(FfmpegToolSet tools)
    {
        _tools = tools;
    }

    public async Task<CompressionResult> CompressAsync(
        CompressionRequest request,
        TimeSpan? duration,
        IProgress<CompressionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath)) ?? Directory.GetCurrentDirectory());

        var psi = new ProcessStartInfo
        {
            FileName = _tools.FfmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in FfmpegArgumentBuilder.Build(request))
        {
            psi.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        try
        {
            if (!process.Start())
            {
                return CompressionResult.Failure(request.OutputPath, "无法启动 ffmpeg。", stopwatch.Elapsed);
            }

            var stdoutTask = ReadProgressAsync(process.StandardOutput, duration, progress, cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                stopwatch.Stop();
                return CompressionResult.Cancelled(request.OutputPath, stopwatch.Elapsed);
            }

            try
            {
                await stdoutTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                stopwatch.Stop();
                return CompressionResult.Cancelled(request.OutputPath, stopwatch.Elapsed);
            }

            var stderr = await stderrTask.ConfigureAwait(false);
            stopwatch.Stop();

            if (process.ExitCode == 0)
            {
                progress?.Report(new CompressionProgress(100, duration ?? TimeSpan.Zero, "完成"));
                return CompressionResult.Success(request.OutputPath, stopwatch.Elapsed);
            }

            var message = string.IsNullOrWhiteSpace(stderr) ? $"ffmpeg 退出码：{process.ExitCode}" : stderr.Trim();
            return CompressionResult.Failure(request.OutputPath, message, stopwatch.Elapsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            return CompressionResult.Failure(request.OutputPath, ex.Message, stopwatch.Elapsed);
        }
    }

    private static async Task ReadProgressAsync(
        StreamReader reader,
        TimeSpan? duration,
        IProgress<CompressionProgress>? progress,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (line.StartsWith("out_time_ms=", StringComparison.OrdinalIgnoreCase)
                && long.TryParse(line["out_time_ms=".Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var microseconds))
            {
                var position = TimeSpan.FromMilliseconds(microseconds / 1000d);
                var percentage = duration is { TotalMilliseconds: > 0 }
                    ? Math.Clamp(position.TotalMilliseconds / duration.Value.TotalMilliseconds * 100d, 0d, 99.8d)
                    : 0d;
                progress?.Report(new CompressionProgress(percentage, position));
            }
            else if (line.StartsWith("progress=", StringComparison.OrdinalIgnoreCase))
            {
                var value = line["progress=".Length..];
                if (value == "end")
                {
                    progress?.Report(new CompressionProgress(100d, duration ?? TimeSpan.Zero, "完成"));
                }
            }
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup during cancellation.
        }
    }
}
