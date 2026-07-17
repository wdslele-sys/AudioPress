namespace AudioPress.Core.Presets;

public sealed record CompressionPreset(
    string Id,
    string Name,
    string Description,
    AudioFormat Format,
    int? BitrateKbps = null,
    int? SampleRateHz = null,
    int? Channels = null,
    int? VbrQuality = null,
    int? FlacCompressionLevel = null,
    int? WavBitDepth = null)
{
    public override string ToString() => Name;
}

