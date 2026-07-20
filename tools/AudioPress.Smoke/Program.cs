using AudioPress.Core.Compression;
using AudioPress.Core.Media;
using AudioPress.Core.Presets;
using AudioPress.Core.Tools;

if (args.Length >= 2 && (string.Equals(args[0], "--probe", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(args[0], "--probe-many", StringComparison.OrdinalIgnoreCase)))
{
    var probeTools = FfmpegToolLocator.Resolve();
    var fileProbe = new MediaProbeService(probeTools);
    var files = string.Equals(args[0], "--probe-many", StringComparison.OrdinalIgnoreCase)
        ? args.Skip(1)
        : args.Take(2).Skip(1);
    var results = await Task.WhenAll(files.Select(async file =>
    {
        var fullPath = Path.GetFullPath(file);
        var info = await fileProbe.ProbeAsync(fullPath, CancellationToken.None);
        return (fullPath, info);
    }));
    foreach (var probeResult in results)
    {
        Console.WriteLine($"{Path.GetFileName(probeResult.fullPath)}: {probeResult.info.Duration}, {probeResult.info.PrimaryCodec}, {probeResult.info.FileSizeBytes}");
    }
    return 0;
}

var workingDirectory = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(Path.GetTempPath(), "AudioPress-Smoke-" + Guid.NewGuid().ToString("N"));

Directory.CreateDirectory(workingDirectory);

var inputPath = Path.Combine(workingDirectory, "sample.wav");
var outputPath = Path.Combine(workingDirectory, "sample.m4a");

SineWaveWriter.WriteMono16BitWav(inputPath, TimeSpan.FromSeconds(1), sampleRate: 44100, frequency: 440);

var tools = FfmpegToolLocator.Resolve();
Console.WriteLine($"ffmpeg: {tools.FfmpegPath}");
Console.WriteLine($"ffprobe: {tools.FfprobePath}");

var probe = new MediaProbeService(tools);
var inputInfo = await probe.ProbeAsync(inputPath, CancellationToken.None);
Console.WriteLine($"input duration: {inputInfo.Duration}");

var compressor = new FfmpegCompressionService(tools);
var preset = PresetCatalog.All.First(item => item.Id == "music-balanced");
var request = new CompressionRequest(
    inputPath,
    outputPath,
    preset,
    new CompressionOverrides(BitrateKbps: 96),
    PreserveMetadata: true,
    PreserveCoverArt: false);

var result = await compressor.CompressAsync(
    request,
    inputInfo.Duration,
    new Progress<CompressionProgress>(progress => Console.WriteLine($"progress: {progress.Percentage:0.0}%")),
    CancellationToken.None);

if (!result.Succeeded || !File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
{
    Console.Error.WriteLine(result.ErrorMessage ?? "Smoke test did not produce an output file.");
    return 1;
}

var outputInfo = await probe.ProbeAsync(outputPath, CancellationToken.None);
Console.WriteLine($"output duration: {outputInfo.Duration}");
Console.WriteLine($"output path: {outputPath}");
return 0;

internal static class SineWaveWriter
{
    public static void WriteMono16BitWav(string path, TimeSpan duration, int sampleRate, double frequency)
    {
        var sampleCount = (int)(duration.TotalSeconds * sampleRate);
        const short amplitude = short.MaxValue / 4;
        var dataLength = sampleCount * sizeof(short);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);

        for (var index = 0; index < sampleCount; index++)
        {
            var sample = (short)(Math.Sin(2 * Math.PI * frequency * index / sampleRate) * amplitude);
            writer.Write(sample);
        }
    }
}

