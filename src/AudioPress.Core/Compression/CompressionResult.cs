namespace AudioPress.Core.Compression;

public sealed record CompressionResult(
    bool Succeeded,
    bool Canceled,
    string OutputPath,
    string? ErrorMessage,
    TimeSpan Elapsed)
{
    public static CompressionResult Success(string outputPath, TimeSpan elapsed)
        => new(true, false, outputPath, null, elapsed);

    public static CompressionResult Failure(string outputPath, string errorMessage, TimeSpan elapsed)
        => new(false, false, outputPath, errorMessage, elapsed);

    public static CompressionResult Cancelled(string outputPath, TimeSpan elapsed)
        => new(false, true, outputPath, null, elapsed);
}

