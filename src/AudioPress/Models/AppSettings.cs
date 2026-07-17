using AudioPress.Core.Compression;
using AudioPress.Core.Presets;

namespace AudioPress.Models;

public sealed class AppSettings
{
    public string SelectedPresetId { get; set; } = "music-balanced";

    public AudioFormat OutputFormat { get; set; } = AudioFormat.M4A;

    public string? OutputDirectory { get; set; }

    public bool UseSourceCompressedFolder { get; set; } = true;

    public SameNamePolicy SameNamePolicy { get; set; } = SameNamePolicy.AutoNumber;

    public bool PreserveMetadata { get; set; } = true;

    public bool PreserveCoverArt { get; set; } = true;

    public string? BitrateKbps { get; set; }

    public string? SampleRateHz { get; set; }

    public string? Channels { get; set; }

    public string? VbrQuality { get; set; }

    public string? FlacCompressionLevel { get; set; }

    public string? WavBitDepth { get; set; }
}

