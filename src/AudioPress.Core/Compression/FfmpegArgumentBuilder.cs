using AudioPress.Core.Presets;

namespace AudioPress.Core.Compression;

public static class FfmpegArgumentBuilder
{
    public static IReadOnlyList<string> Build(CompressionRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputPath);

        var args = new List<string>
        {
            "-hide_banner",
            "-nostdin",
            "-y",
            "-progress",
            "pipe:1",
            "-nostats",
            "-i",
            request.InputPath,
            "-map",
            "0:a:0"
        };

        if (request.PreserveCoverArt && request.Preset.Format.SupportsEmbeddedCoverArt())
        {
            args.AddRange(["-map", "0:v?", "-c:v", "copy", "-disposition:v", "attached_pic"]);
        }

        if (request.PreserveMetadata)
        {
            args.AddRange(["-map_metadata", "0"]);
        }
        else
        {
            args.AddRange(["-map_metadata", "-1"]);
        }

        AddAudioCodecArgs(args, request);

        var sampleRate = request.Overrides.SampleRateHz ?? request.Preset.SampleRateHz;
        if (sampleRate is > 0)
        {
            args.AddRange(["-ar", sampleRate.Value.ToString()]);
        }

        var channels = request.Overrides.Channels ?? request.Preset.Channels;
        if (channels is > 0)
        {
            args.AddRange(["-ac", channels.Value.ToString()]);
        }

        args.Add(request.OutputPath);
        return args;
    }

    private static void AddAudioCodecArgs(List<string> args, CompressionRequest request)
    {
        var preset = request.Preset;
        var overrides = request.Overrides;
        var bitrate = overrides.BitrateKbps ?? preset.BitrateKbps;
        var vbrQuality = overrides.VbrQuality ?? preset.VbrQuality;

        switch (preset.Format)
        {
            case AudioFormat.Mp3:
                args.AddRange(["-c:a", "libmp3lame"]);
                if (vbrQuality is >= 0 and <= 9)
                {
                    args.AddRange(["-q:a", vbrQuality.Value.ToString()]);
                }
                else if (bitrate is > 0)
                {
                    args.AddRange(["-b:a", $"{bitrate.Value}k"]);
                }

                args.AddRange(["-id3v2_version", "3"]);
                break;

            case AudioFormat.M4A:
                args.AddRange(["-c:a", "aac"]);
                if (bitrate is > 0)
                {
                    args.AddRange(["-b:a", $"{bitrate.Value}k"]);
                }

                args.AddRange(["-movflags", "+faststart"]);
                break;

            case AudioFormat.Aac:
                args.AddRange(["-c:a", "aac"]);
                if (bitrate is > 0)
                {
                    args.AddRange(["-b:a", $"{bitrate.Value}k"]);
                }

                break;

            case AudioFormat.Opus:
                args.AddRange(["-c:a", "libopus"]);
                if (bitrate is > 0)
                {
                    args.AddRange(["-b:a", $"{bitrate.Value}k"]);
                }

                args.AddRange(["-vbr", "on", "-compression_level", "10"]);
                break;

            case AudioFormat.Ogg:
                args.AddRange(["-c:a", "libvorbis"]);
                if (vbrQuality is >= -1 and <= 10)
                {
                    args.AddRange(["-q:a", vbrQuality.Value.ToString()]);
                }
                else if (bitrate is > 0)
                {
                    args.AddRange(["-b:a", $"{bitrate.Value}k"]);
                }

                break;

            case AudioFormat.Flac:
                args.AddRange(["-c:a", "flac"]);
                var flacLevel = overrides.FlacCompressionLevel ?? preset.FlacCompressionLevel ?? 5;
                args.AddRange(["-compression_level", Math.Clamp(flacLevel, 0, 12).ToString()]);
                break;

            case AudioFormat.Wav:
                var bitDepth = overrides.WavBitDepth ?? preset.WavBitDepth ?? 16;
                var codec = bitDepth >= 24 ? "pcm_s24le" : "pcm_s16le";
                args.AddRange(["-c:a", codec]);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(request), preset.Format, "Unknown output audio format.");
        }
    }
}

