namespace AudioPress.Core.Utilities;

public sealed record OutputPathResolution(
    string? OutputPath,
    bool ShouldSkip,
    string? Message);

