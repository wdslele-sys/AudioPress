namespace AudioPress.Core.Presets;

public enum AudioFormat
{
    Mp3,
    M4A,
    Aac,
    Opus,
    Ogg,
    Flac,
    Wav
}

public static class AudioFormatExtensions
{
    public static string GetExtension(this AudioFormat format) => format switch
    {
        AudioFormat.Mp3 => ".mp3",
        AudioFormat.M4A => ".m4a",
        AudioFormat.Aac => ".aac",
        AudioFormat.Opus => ".opus",
        AudioFormat.Ogg => ".ogg",
        AudioFormat.Flac => ".flac",
        AudioFormat.Wav => ".wav",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    public static string GetDisplayName(this AudioFormat format) => format switch
    {
        AudioFormat.Mp3 => "MP3",
        AudioFormat.M4A => "M4A / AAC",
        AudioFormat.Aac => "AAC",
        AudioFormat.Opus => "Opus",
        AudioFormat.Ogg => "OGG Vorbis",
        AudioFormat.Flac => "FLAC",
        AudioFormat.Wav => "WAV",
        _ => format.ToString()
    };

    public static bool SupportsEmbeddedCoverArt(this AudioFormat format) => format is AudioFormat.Mp3 or AudioFormat.M4A or AudioFormat.Flac;
}

