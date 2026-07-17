using AudioPress.Core.Presets;

namespace AudioPress.Core.Compression;

public sealed record CompressionRequest(
    string InputPath,
    string OutputPath,
    CompressionPreset Preset,
    CompressionOverrides Overrides,
    bool PreserveMetadata,
    bool PreserveCoverArt);

