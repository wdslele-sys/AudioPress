using AudioPress.Core.Presets;

namespace AudioPress.Models;

public sealed record FormatOption(AudioFormat Format, string DisplayName)
{
    public override string ToString() => DisplayName;
}

