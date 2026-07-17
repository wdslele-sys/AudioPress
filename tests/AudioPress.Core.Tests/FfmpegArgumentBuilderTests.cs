using AudioPress.Core.Compression;
using AudioPress.Core.Presets;
using Xunit;

namespace AudioPress.Core.Tests;

public sealed class FfmpegArgumentBuilderTests
{
    [Fact]
    public void Build_AddsSafeInputAndOutputArguments()
    {
        var request = new CompressionRequest(
            @"C:\音频 文件\input file.wav",
            @"D:\输出\input file.mp3",
            PresetCatalog.All.First(preset => preset.Id == "mp3-compatible"),
            new CompressionOverrides(),
            PreserveMetadata: true,
            PreserveCoverArt: true);

        var args = FfmpegArgumentBuilder.Build(request);

        Assert.Contains(@"C:\音频 文件\input file.wav", args);
        Assert.Contains(@"D:\输出\input file.mp3", args);
        Assert.Contains("libmp3lame", args);
        Assert.Contains("-map_metadata", args);
        Assert.Contains("0:v?", args);
    }

    [Fact]
    public void Build_UsesOverrides_WhenProvided()
    {
        var request = new CompressionRequest(
            "input.wav",
            "output.opus",
            PresetCatalog.All.First(preset => preset.Id == "speech-clear") with { Format = AudioFormat.Opus },
            new CompressionOverrides(BitrateKbps: 32, SampleRateHz: 16000, Channels: 1),
            PreserveMetadata: false,
            PreserveCoverArt: false);

        var args = FfmpegArgumentBuilder.Build(request);

        Assert.Contains("libopus", args);
        Assert.Contains("32k", args);
        Assert.Contains("16000", args);
        Assert.Contains("1", args);
        Assert.Contains("-1", args);
    }
}
