namespace AudioPress.Core.Compression;

public sealed record CompressionOverrides(
    int? BitrateKbps = null,
    int? SampleRateHz = null,
    int? Channels = null,
    int? VbrQuality = null,
    int? FlacCompressionLevel = null,
    int? WavBitDepth = null);

