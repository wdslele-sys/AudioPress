namespace AudioPress.Core.Media;

public sealed record MediaInfo(
    TimeSpan? Duration,
    string? Title,
    string? Artist,
    string? Album,
    string? PrimaryCodec,
    long? FileSizeBytes);

