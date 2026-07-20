using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using AudioPress.Commands;
using AudioPress.Core.Compression;
using AudioPress.Core.Media;
using AudioPress.Core.Presets;
using AudioPress.Core.Tools;
using AudioPress.Core.Utilities;
using AudioPress.Models;
using AudioPress.Services;

namespace AudioPress.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly SettingsService _settingsService = new();
    private readonly LogService _logService = new();
    private readonly MediaProbeService _probeService;
    private readonly FfmpegCompressionService _compressionService;
    private readonly HashSet<string> _knownPaths = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _queueCancellation;
    private TaskCompletionSource<bool>? _processingCompletion;

    private CompressionPreset _selectedPreset = PresetCatalog.Default;
    private ProcessingModeOption _selectedProcessingMode;
    private FormatOption _selectedOutputFormat;
    private SameNamePolicyOption _selectedSameNamePolicy;
    private string? _outputDirectory;
    private bool _useSourceCompressedFolder = true;
    private bool _recursiveFolderScan = true;
    private bool _preserveMetadata = true;
    private bool _preserveCoverArt = true;
    private bool _isProcessing;
    private string _bitrateKbps = string.Empty;
    private string _sampleRateHz = string.Empty;
    private string _channels = string.Empty;
    private string _vbrQuality = string.Empty;
    private string _flacCompressionLevel = string.Empty;
    private string _wavBitDepth = string.Empty;
    private string _statusLine = "准备就绪";
    private string _logText = string.Empty;

    public MainWindowViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        var tools = FfmpegToolLocator.Resolve();
        _probeService = new MediaProbeService(tools);
        _compressionService = new FfmpegCompressionService(tools);

        OutputFormats = Enum.GetValues<AudioFormat>()
            .Select(format => new FormatOption(format, format.GetDisplayName()))
            .ToList();
        ProcessingModes =
        [
            new ProcessingModeOption(ProcessingMode.Compress, "批量压缩"),
            new ProcessingModeOption(ProcessingMode.ExtractAudio, "提取音频")
        ];
        BitrateOptions = ["48", "64", "96", "128", "160", "192", "256", "320"];
        SameNamePolicies =
        [
            new SameNamePolicyOption(SameNamePolicy.AutoNumber, "自动编号"),
            new SameNamePolicyOption(SameNamePolicy.Skip, "跳过"),
            new SameNamePolicyOption(SameNamePolicy.Overwrite, "覆盖")
        ];

        _selectedProcessingMode = ProcessingModes[0];
        _selectedOutputFormat = OutputFormats.First(option => option.Format == _selectedPreset.Format);
        _selectedSameNamePolicy = SameNamePolicies[0];
        ApplyPresetDefaults(_selectedPreset);

        Jobs.CollectionChanged += JobsOnCollectionChanged;

        AddFilesCommand = new AsyncRelayCommand(AddFilesAsync, () => !IsProcessing);
        AddFolderCommand = new AsyncRelayCommand(AddFolderAsync, () => !IsProcessing);
        BrowseOutputCommand = new RelayCommand(BrowseOutputFolder);
        StartCommand = new AsyncRelayCommand(StartAsync, () => Jobs.Any(job => job.CanProcess) && !IsProcessing);
        CancelAllCommand = new RelayCommand(CancelAll, () => IsProcessing);
        ClearCommand = new RelayCommand(ClearJobs, () => !IsProcessing && Jobs.Count > 0);
        RetryFailedCommand = new AsyncRelayCommand(RetryFailedAsync, () => Jobs.Any(job => job.State == JobState.Failed) && !IsProcessing);

        _ = LoadSettingsAsync();
        _ = LogAsync($"FFmpeg: {tools.FfmpegPath}");
        _ = LogAsync($"FFprobe: {tools.FfprobePath}");
    }

    public IReadOnlyList<CompressionPreset> Presets => PresetCatalog.All;

    public IReadOnlyList<ProcessingModeOption> ProcessingModes { get; }

    public IReadOnlyList<string> BitrateOptions { get; }

    public IReadOnlyList<FormatOption> OutputFormats { get; }

    public IReadOnlyList<SameNamePolicyOption> SameNamePolicies { get; }

    public ObservableCollection<AudioJobViewModel> Jobs { get; } = [];

    public AsyncRelayCommand AddFilesCommand { get; }

    public AsyncRelayCommand AddFolderCommand { get; }

    public RelayCommand BrowseOutputCommand { get; }

    public AsyncRelayCommand StartCommand { get; }

    public RelayCommand CancelAllCommand { get; }

    public RelayCommand ClearCommand { get; }

    public AsyncRelayCommand RetryFailedCommand { get; }

    public ProcessingModeOption SelectedProcessingMode
    {
        get => _selectedProcessingMode;
        set
        {
            if (value is not null && SetProperty(ref _selectedProcessingMode, value))
            {
                OnPropertyChanged(nameof(IsExtractMode));
                OnPropertyChanged(nameof(StartButtonText));
                OnPropertyChanged(nameof(SettingsHeader));
                OnPropertyChanged(nameof(SourceFolderLabel));
                OnPropertyChanged(nameof(EstimatedOutputSizeText));
            }
        }
    }

    public bool IsExtractMode => SelectedProcessingMode.Mode == ProcessingMode.ExtractAudio;

    public string StartButtonText => IsExtractMode ? "开始提取" : "开始压缩";

    public string SettingsHeader => IsExtractMode ? "音频提取设置" : "压缩设置";

    public string SourceFolderLabel => IsExtractMode
        ? "源目录下的 ExtractedAudio 文件夹"
        : "源目录下的 Compressed 文件夹";

    public CompressionPreset SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (value is not null && SetProperty(ref _selectedPreset, value))
            {
                SelectedOutputFormat = OutputFormats.First(option => option.Format == value.Format);
                ApplyPresetDefaults(value);
                OnPropertyChanged(nameof(SelectedPresetDescription));
            }
        }
    }

    public string SelectedPresetDescription => SelectedPreset.Description;

    public FormatOption SelectedOutputFormat
    {
        get => _selectedOutputFormat;
        set
        {
            if (value is not null && SetProperty(ref _selectedOutputFormat, value))
            {
                OnPropertyChanged(nameof(EstimatedOutputSizeText));
            }
        }
    }

    public SameNamePolicyOption SelectedSameNamePolicy
    {
        get => _selectedSameNamePolicy;
        set
        {
            if (value is not null)
            {
                SetProperty(ref _selectedSameNamePolicy, value);
            }
        }
    }

    public string? OutputDirectory
    {
        get => _outputDirectory;
        set => SetProperty(ref _outputDirectory, value);
    }

    public bool UseSourceCompressedFolder
    {
        get => _useSourceCompressedFolder;
        set
        {
            if (SetProperty(ref _useSourceCompressedFolder, value))
            {
                OnPropertyChanged(nameof(CanChooseOutputDirectory));
            }
        }
    }

    public bool CanChooseOutputDirectory => !UseSourceCompressedFolder;

    public bool RecursiveFolderScan
    {
        get => _recursiveFolderScan;
        set => SetProperty(ref _recursiveFolderScan, value);
    }

    public bool PreserveMetadata
    {
        get => _preserveMetadata;
        set => SetProperty(ref _preserveMetadata, value);
    }

    public bool PreserveCoverArt
    {
        get => _preserveCoverArt;
        set => SetProperty(ref _preserveCoverArt, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        private set
        {
            if (SetProperty(ref _isProcessing, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string BitrateKbps
    {
        get => _bitrateKbps;
        set
        {
            if (SetProperty(ref _bitrateKbps, value))
            {
                OnPropertyChanged(nameof(EstimatedOutputSizeText));
            }
        }
    }

    public string SampleRateHz
    {
        get => _sampleRateHz;
        set
        {
            if (SetProperty(ref _sampleRateHz, value))
            {
                OnPropertyChanged(nameof(EstimatedOutputSizeText));
            }
        }
    }

    public string Channels
    {
        get => _channels;
        set
        {
            if (SetProperty(ref _channels, value))
            {
                OnPropertyChanged(nameof(EstimatedOutputSizeText));
            }
        }
    }

    public string VbrQuality
    {
        get => _vbrQuality;
        set => SetProperty(ref _vbrQuality, value);
    }

    public string FlacCompressionLevel
    {
        get => _flacCompressionLevel;
        set => SetProperty(ref _flacCompressionLevel, value);
    }

    public string WavBitDepth
    {
        get => _wavBitDepth;
        set
        {
            if (SetProperty(ref _wavBitDepth, value))
            {
                OnPropertyChanged(nameof(EstimatedOutputSizeText));
            }
        }
    }

    public string StatusLine
    {
        get => _statusLine;
        private set => SetProperty(ref _statusLine, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
    }

    public int TotalCount => Jobs.Count;

    public int FinishedCount => Jobs.Count(job => job.State is JobState.Completed or JobState.Failed or JobState.Skipped or JobState.Canceled);

    public string EstimatedOutputSizeText
    {
        get
        {
            var totalSeconds = Jobs.Select(job => job.MediaInfo?.Duration?.TotalSeconds ?? 0d).Sum();
            var totalInputBytes = Jobs.Sum(job =>
                job.MediaInfo?.FileSizeBytes
                ?? (File.Exists(job.InputPath) ? new FileInfo(job.InputPath).Length : 0L));
            return totalSeconds <= 0
                ? "预计体积：添加并识别文件后显示"
                : EstimateOutputSize(totalSeconds, totalInputBytes);
        }
    }

    public async Task AddDroppedPathsAsync(IEnumerable<string> paths)
        => await AddPathsAsync(paths, RecursiveFolderScan).ConfigureAwait(true);

    public async Task SaveSettingsAsync()
    {
        var settings = new AppSettings
        {
            ProcessingMode = SelectedProcessingMode.Mode,
            SelectedPresetId = SelectedPreset.Id,
            OutputFormat = SelectedOutputFormat.Format,
            OutputDirectory = OutputDirectory,
            UseSourceCompressedFolder = UseSourceCompressedFolder,
            SameNamePolicy = SelectedSameNamePolicy.Policy,
            PreserveMetadata = PreserveMetadata,
            PreserveCoverArt = PreserveCoverArt,
            BitrateKbps = BitrateKbps,
            SampleRateHz = SampleRateHz,
            Channels = Channels,
            VbrQuality = VbrQuality,
            FlacCompressionLevel = FlacCompressionLevel,
            WavBitDepth = WavBitDepth
        };

        await _settingsService.SaveAsync(settings).ConfigureAwait(false);
    }

    public async Task CancelProcessingAsync()
    {
        if (!IsProcessing)
        {
            return;
        }

        var completion = _processingCompletion;
        CancelAll();
        if (completion is not null)
        {
            await completion.Task.ConfigureAwait(true);
        }
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
        SelectedProcessingMode = ProcessingModes.FirstOrDefault(option => option.Mode == settings.ProcessingMode)
            ?? ProcessingModes[0];
        SelectedPreset = PresetCatalog.FindOrDefault(settings.SelectedPresetId);
        SelectedOutputFormat = OutputFormats.FirstOrDefault(option => option.Format == settings.OutputFormat)
            ?? OutputFormats.First(option => option.Format == SelectedPreset.Format);
        OutputDirectory = settings.OutputDirectory;
        UseSourceCompressedFolder = settings.UseSourceCompressedFolder;
        SelectedSameNamePolicy = SameNamePolicies.FirstOrDefault(option => option.Policy == settings.SameNamePolicy) ?? SameNamePolicies[0];
        PreserveMetadata = settings.PreserveMetadata;
        PreserveCoverArt = settings.PreserveCoverArt;
        BitrateKbps = settings.BitrateKbps ?? BitrateKbps;
        SampleRateHz = settings.SampleRateHz ?? SampleRateHz;
        Channels = settings.Channels ?? Channels;
        VbrQuality = settings.VbrQuality ?? VbrQuality;
        FlacCompressionLevel = settings.FlacCompressionLevel ?? FlacCompressionLevel;
        WavBitDepth = settings.WavBitDepth ?? WavBitDepth;
    }

    private async Task AddFilesAsync()
    {
        var files = _dialogService.PickMediaFiles(IsExtractMode);
        await AddPathsAsync(files, recursive: false).ConfigureAwait(true);
    }

    private async Task AddFolderAsync()
    {
        var folder = _dialogService.PickFolder(OutputDirectory);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            await AddPathsAsync([folder], RecursiveFolderScan).ConfigureAwait(true);
        }
    }

    private async Task AddPathsAsync(IEnumerable<string> paths, bool recursive)
    {
        var files = AudioFileScanner.ScanFiles(paths, recursive, includeVideo: IsExtractMode);
        var added = 0;

        foreach (var file in files)
        {
            if (!_knownPaths.Add(file))
            {
                continue;
            }

            var job = new AudioJobViewModel(file);
            Jobs.Add(job);
            added++;
            _ = ProbeJobAsync(job);
        }

        await LogAsync(added == 0 ? "没有新增可处理的媒体文件。" : $"已添加 {added} 个媒体文件。").ConfigureAwait(true);
        UpdateCounts();
        RaiseCommandStates();
    }

    private async Task ProbeJobAsync(AudioJobViewModel job)
    {
        if (job.State is JobState.Running or JobState.Completed)
        {
            return;
        }

        job.State = JobState.Probing;
        job.Message = "正在读取音频信息";

        try
        {
            var mediaInfo = await _probeService.ProbeAsync(job.InputPath, CancellationToken.None).ConfigureAwait(true);
            job.ApplyMediaInfo(mediaInfo);
            job.MarkReady();
        }
        catch (Exception ex)
        {
            job.MarkFailed($"识别失败：{TrimMessage(ex.Message)}");
            await LogAsync($"{job.FileName} 识别失败：{TrimMessage(ex.Message)}").ConfigureAwait(true);
        }
        finally
        {
            UpdateCounts();
            OnPropertyChanged(nameof(EstimatedOutputSizeText));
            RaiseCommandStates();
        }
    }

    private async Task StartAsync()
    {
        if (IsProcessing)
        {
            return;
        }

        var queueCancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queueCancellation = queueCancellation;
        _processingCompletion = completion;
        IsProcessing = true;

        try
        {
            await SaveSettingsAsync().ConfigureAwait(true);
            StatusLine = "正在处理队列";
            await LogAsync("开始批量处理。").ConfigureAwait(true);

            foreach (var job in Jobs.Where(job => job.CanProcess).ToList())
            {
                if (queueCancellation.IsCancellationRequested)
                {
                    break;
                }

                await ProcessJobAsync(job, queueCancellation.Token).ConfigureAwait(true);
                UpdateCounts();
            }
        }
        catch (Exception ex)
        {
            StatusLine = $"处理失败：{TrimMessage(ex.Message)}";
            try
            {
                await LogAsync($"队列处理失败：{TrimMessage(ex.Message)}").ConfigureAwait(true);
            }
            catch
            {
                // A logging failure must not crash the UI.
            }
        }
        finally
        {
            IsProcessing = false;
            queueCancellation.Dispose();
            if (ReferenceEquals(_queueCancellation, queueCancellation))
            {
                _queueCancellation = null;
            }

            StatusLine = "队列处理结束";
            try
            {
                await LogAsync("队列处理结束。").ConfigureAwait(true);
            }
            catch
            {
                // Logging failure must not leave the processing state stuck.
            }
            finally
            {
                UpdateCounts();
                if (ReferenceEquals(_processingCompletion, completion))
                {
                    _processingCompletion = null;
                }

                completion.TrySetResult(true);
            }
        }
    }

    private async Task RetryFailedAsync()
    {
        foreach (var job in Jobs.Where(job => job.State == JobState.Failed))
        {
            job.ResetForRetry();
        }

        await StartAsync().ConfigureAwait(true);
    }

    private async Task ProcessJobAsync(AudioJobViewModel job, CancellationToken queueToken)
    {
        if (job.State == JobState.Probing)
        {
            await ProbeJobAsync(job).ConfigureAwait(true);
        }

        if (job.MediaInfo is null)
        {
            try
            {
                var mediaInfo = await _probeService.ProbeAsync(job.InputPath, queueToken).ConfigureAwait(true);
                job.ApplyMediaInfo(mediaInfo);
                OnPropertyChanged(nameof(EstimatedOutputSizeText));
            }
            catch (Exception ex)
            {
                job.MarkFailed($"识别失败：{TrimMessage(ex.Message)}");
                await LogAsync($"{job.FileName} 识别失败：{TrimMessage(ex.Message)}").ConfigureAwait(true);
                return;
            }
        }

        var preset = SelectedPreset with { Format = SelectedOutputFormat.Format };
        var output = OutputPathResolver.Resolve(
            job.InputPath,
            OutputDirectory,
            UseSourceCompressedFolder,
            preset.Format,
            SelectedSameNamePolicy.Policy,
            IsExtractMode ? "ExtractedAudio" : "Compressed");

        if (output.ShouldSkip || string.IsNullOrWhiteSpace(output.OutputPath))
        {
            job.MarkSkipped(output.Message ?? "已跳过。");
            await LogAsync($"{job.FileName} 已跳过：{job.Message}").ConfigureAwait(true);
            return;
        }

        job.OutputPath = output.OutputPath;
        job.PrepareForRun();
        await LogAsync($"开始处理：{job.InputPath}").ConfigureAwait(true);

        using var jobCancellation = CancellationTokenSource.CreateLinkedTokenSource(queueToken);
        job.AttachCancellation(jobCancellation);

        try
        {
            var request = new CompressionRequest(
                job.InputPath,
                output.OutputPath,
                preset,
                BuildOverrides(),
                PreserveMetadata,
                PreserveCoverArt && !IsExtractMode);

            var progress = new Progress<CompressionProgress>(report =>
            {
                if (report.Percentage > 0)
                {
                    job.UpdateProgress(report.Percentage, report.StatusMessage);
                }
                else if (!string.IsNullOrWhiteSpace(report.StatusMessage))
                {
                    job.Message = report.StatusMessage;
                }
            });

            var result = await _compressionService.CompressAsync(
                request,
                job.MediaInfo!.Duration,
                progress,
                jobCancellation.Token).ConfigureAwait(true);

            if (result.Canceled)
            {
                job.MarkCanceled();
                await LogAsync($"{job.FileName} 已取消。").ConfigureAwait(true);
                return;
            }

            if (result.Succeeded)
            {
                job.MarkCompleted(result.OutputPath);
                await LogAsync($"{job.FileName} 完成：{result.OutputPath}").ConfigureAwait(true);
                return;
            }

            job.MarkFailed(TrimMessage(result.ErrorMessage ?? "压缩失败。"));
            await LogAsync($"{job.FileName} 失败：{job.Message}").ConfigureAwait(true);
        }
        finally
        {
            job.DetachCancellation();
        }
    }

    private CompressionOverrides BuildOverrides()
        => new(
            BitrateKbps: ParseNullablePositiveInt(BitrateKbps),
            SampleRateHz: ParseNullablePositiveInt(SampleRateHz),
            Channels: ParseNullablePositiveInt(Channels),
            VbrQuality: ParseNullableInt(VbrQuality),
            FlacCompressionLevel: ParseNullableInt(FlacCompressionLevel),
            WavBitDepth: ParseNullablePositiveInt(WavBitDepth));

    private void BrowseOutputFolder()
    {
        var folder = _dialogService.PickFolder(OutputDirectory);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            OutputDirectory = folder;
            UseSourceCompressedFolder = false;
        }
    }

    private void CancelAll()
    {
        _queueCancellation?.Cancel();
        foreach (var job in Jobs.Where(job => job.State == JobState.Running))
        {
            if (job.CancelCommand.CanExecute(null))
            {
                job.CancelCommand.Execute(null);
            }
        }

        StatusLine = "正在取消";
    }

    private void ClearJobs()
    {
        Jobs.Clear();
        _knownPaths.Clear();
        StatusLine = "已清空队列";
        UpdateCounts();
        RaiseCommandStates();
    }

    private void ApplyPresetDefaults(CompressionPreset preset)
    {
        BitrateKbps = preset.BitrateKbps?.ToString() ?? string.Empty;
        SampleRateHz = preset.SampleRateHz?.ToString() ?? string.Empty;
        Channels = preset.Channels?.ToString() ?? string.Empty;
        VbrQuality = preset.VbrQuality?.ToString() ?? string.Empty;
        FlacCompressionLevel = preset.FlacCompressionLevel?.ToString() ?? string.Empty;
        WavBitDepth = preset.WavBitDepth?.ToString() ?? string.Empty;
    }

    private async Task LogAsync(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogText = string.IsNullOrWhiteSpace(LogText) ? line : LogText + Environment.NewLine + line;
        await _logService.WriteAsync(message).ConfigureAwait(false);
    }

    private void JobsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCounts();
        OnPropertyChanged(nameof(EstimatedOutputSizeText));
        RaiseCommandStates();
    }

    private void UpdateCounts()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FinishedCount));
        StatusLine = Jobs.Count == 0 ? "准备就绪" : $"{FinishedCount}/{TotalCount} 已结束";
    }

    private void RaiseCommandStates()
    {
        AddFilesCommand.RaiseCanExecuteChanged();
        AddFolderCommand.RaiseCanExecuteChanged();
        StartCommand.RaiseCanExecuteChanged();
        CancelAllCommand.RaiseCanExecuteChanged();
        ClearCommand.RaiseCanExecuteChanged();
        RetryFailedCommand.RaiseCanExecuteChanged();
    }

    private static int? ParseNullablePositiveInt(string? text)
    {
        var value = ParseNullableInt(text);
        return value is > 0 ? value : null;
    }

    private static int? ParseNullableInt(string? text)
        => int.TryParse(text, out var value) ? value : null;

    private string EstimateOutputSize(double totalSeconds, long totalInputBytes)
    {
        var format = SelectedOutputFormat.Format;
        var sampleRate = ParseNullablePositiveInt(SampleRateHz) ?? SelectedPreset.SampleRateHz ?? 44_100;
        var channels = ParseNullablePositiveInt(Channels) ?? SelectedPreset.Channels ?? 2;
        var bitDepth = ParseNullablePositiveInt(WavBitDepth) ?? SelectedPreset.WavBitDepth ?? 16;

        if (format == AudioFormat.Wav)
        {
            var bytes = totalSeconds * sampleRate * channels * bitDepth / 8d;
            var detail = $"预计音频：约 {FileSizeFormatter.Format((long)bytes)}（WAV {sampleRate}Hz / {channels}声道 / {bitDepth}bit）";
            return AppendSpaceSaving(detail, totalInputBytes, (long)bytes);
        }

        if (format == AudioFormat.Flac)
        {
            var pcmBytes = totalSeconds * sampleRate * channels * Math.Max(bitDepth, 16) / 8d;
            var low = (long)(pcmBytes * 0.45d);
            var high = (long)(pcmBytes * 0.70d);
            var detail = $"预计音频：约 {FileSizeFormatter.Format(low)}–{FileSizeFormatter.Format(high)}（FLAC 无损，取决于音源）";
            return AppendSpaceSaving(detail, totalInputBytes, (low + high) / 2);
        }

        var bitrate = ParseNullablePositiveInt(BitrateKbps)
            ?? SelectedPreset.BitrateKbps
            ?? format switch
            {
                AudioFormat.Opus => 96,
                AudioFormat.M4A or AudioFormat.Aac => 128,
                _ => 160
            };
        var encodedBytes = totalSeconds * bitrate * 1000d / 8d * 1.03d;
        var encodedDetail = $"预计音频：约 {FileSizeFormatter.Format((long)encodedBytes)}（{SelectedOutputFormat.DisplayName} / {bitrate} kbps）";
        return AppendSpaceSaving(encodedDetail, totalInputBytes, (long)encodedBytes);
    }

    private string AppendSpaceSaving(string outputEstimate, long totalInputBytes, long estimatedOutputBytes)
    {
        if (!IsExtractMode || totalInputBytes <= 0)
        {
            return outputEstimate;
        }

        var difference = totalInputBytes - estimatedOutputBytes;
        var percentage = Math.Abs(difference) * 100d / totalInputBytes;
        var source = FileSizeFormatter.Format(totalInputBytes);
        return difference >= 0
            ? $"{outputEstimate}；原视频 {source}，预计节省 {FileSizeFormatter.Format(difference)}（{percentage:0.#}%）"
            : $"{outputEstimate}；原视频 {source}，预计增加 {FileSizeFormatter.Format(-difference)}（{percentage:0.#}%）";
    }

    private static string TrimMessage(string message)
    {
        var normalized = message.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        return normalized.Length <= 300 ? normalized : normalized[..300] + "...";
    }
}
