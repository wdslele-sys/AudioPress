namespace AudioPress.Core.Compression;

public sealed record CompressionProgress(
    double Percentage,
    TimeSpan Position,
    string? StatusMessage = null);

