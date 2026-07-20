namespace AudioPress.Models;

public enum ProcessingMode
{
    Compress,
    ExtractAudio
}

public sealed record ProcessingModeOption(ProcessingMode Mode, string DisplayName)
{
    public override string ToString() => DisplayName;
}
