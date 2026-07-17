using AudioPress.Core.Compression;
using AudioPress.Core.Presets;
using AudioPress.Core.Utilities;
using Xunit;

namespace AudioPress.Core.Tests;

public sealed class OutputPathResolverTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "AudioPressTests-" + Guid.NewGuid().ToString("N"));

    public OutputPathResolverTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void Resolve_UsesSourceCompressedFolder_WhenRequested()
    {
        var input = Path.Combine(_directory, "中文 文件.wav");
        File.WriteAllText(input, "test");

        var result = OutputPathResolver.Resolve(input, null, true, AudioFormat.Mp3, SameNamePolicy.AutoNumber);

        Assert.False(result.ShouldSkip);
        Assert.Equal(Path.Combine(_directory, "Compressed", "中文 文件.mp3"), result.OutputPath);
    }

    [Fact]
    public void Resolve_ReturnsSkip_WhenTargetExistsAndPolicyIsSkip()
    {
        var input = Path.Combine(_directory, "voice.wav");
        var outputDirectory = Path.Combine(_directory, "out");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(input, "test");
        File.WriteAllText(Path.Combine(outputDirectory, "voice.m4a"), "existing");

        var result = OutputPathResolver.Resolve(input, outputDirectory, false, AudioFormat.M4A, SameNamePolicy.Skip);

        Assert.True(result.ShouldSkip);
        Assert.Null(result.OutputPath);
    }

    [Fact]
    public void Resolve_AutoNumbers_WhenTargetExists()
    {
        var input = Path.Combine(_directory, "voice.wav");
        var outputDirectory = Path.Combine(_directory, "out");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(input, "test");
        File.WriteAllText(Path.Combine(outputDirectory, "voice.opus"), "existing");

        var result = OutputPathResolver.Resolve(input, outputDirectory, false, AudioFormat.Opus, SameNamePolicy.AutoNumber);

        Assert.False(result.ShouldSkip);
        Assert.Equal(Path.Combine(outputDirectory, "voice (1).opus"), result.OutputPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
