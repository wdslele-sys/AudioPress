namespace AudioPress.Services;

public sealed class LogService
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public string LogPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AudioPress",
        "logs",
        $"AudioPress-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    public async Task WriteAsync(string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath) ?? Directory.GetCurrentDirectory());
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";

        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(LogPath, line).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}

