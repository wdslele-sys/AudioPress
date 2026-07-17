using AudioPress.Core.Compression;

namespace AudioPress.Models;

public sealed record SameNamePolicyOption(SameNamePolicy Policy, string DisplayName)
{
    public override string ToString() => DisplayName;
}

