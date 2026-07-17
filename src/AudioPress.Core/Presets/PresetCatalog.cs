namespace AudioPress.Core.Presets;

public static class PresetCatalog
{
    public static IReadOnlyList<CompressionPreset> All { get; } =
    [
        new(
            "speech-tiny",
            "语音极小",
            "适合会议录音、课程笔记和备忘，优先缩小体积。",
            AudioFormat.Opus,
            BitrateKbps: 24,
            SampleRateHz: 16000,
            Channels: 1),
        new(
            "speech-clear",
            "语音清晰",
            "适合访谈和清晰人声，体积仍然较小。",
            AudioFormat.Opus,
            BitrateKbps: 48,
            SampleRateHz: 24000,
            Channels: 1),
        new(
            "podcast-standard",
            "播客标准",
            "适合长音频节目和口播内容。",
            AudioFormat.Mp3,
            BitrateKbps: 96,
            SampleRateHz: 44100,
            Channels: 2),
        new(
            "music-balanced",
            "音乐均衡",
            "适合日常听歌和一般分享。",
            AudioFormat.M4A,
            BitrateKbps: 160,
            SampleRateHz: 44100,
            Channels: 2),
        new(
            "music-high",
            "音乐高品质",
            "适合保留较好音乐细节。",
            AudioFormat.M4A,
            BitrateKbps: 256,
            SampleRateHz: 48000,
            Channels: 2),
        new(
            "mp3-compatible",
            "MP3兼容",
            "适合车机、老播放器和跨设备分享。",
            AudioFormat.Mp3,
            BitrateKbps: 192,
            SampleRateHz: 44100,
            Channels: 2),
        new(
            "lossless-archive",
            "无损归档",
            "转为 FLAC 无损压缩，适合长期保存。",
            AudioFormat.Flac,
            SampleRateHz: null,
            Channels: null,
            FlacCompressionLevel: 8),
        new(
            "wav-editing",
            "WAV编辑",
            "转为标准 WAV，方便导入音频编辑软件。",
            AudioFormat.Wav,
            SampleRateHz: 48000,
            Channels: 2,
            WavBitDepth: 16)
    ];

    public static CompressionPreset Default => All[3];

    public static CompressionPreset FindOrDefault(string? id)
        => All.FirstOrDefault(preset => string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Default;
}

