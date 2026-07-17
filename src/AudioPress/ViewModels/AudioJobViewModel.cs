using System.IO;
using System.Windows.Input;
using AudioPress.Commands;
using AudioPress.Core.Media;
using AudioPress.Core.Utilities;

namespace AudioPress.ViewModels;

public sealed class AudioJobViewModel : ObservableObject
{
    private JobState _state = JobState.Pending;
    private double _progress;
    private string _message = "等待处理";
    private string? _outputPath;
    private string _durationText = "-";
    private string _sizeText;
    private string _codecText = "-";
    private CancellationTokenSource? _cancellationTokenSource;

    public AudioJobViewModel(string inputPath)
    {
        InputPath = inputPath;
        FileName = Path.GetFileName(inputPath);
        _sizeText = File.Exists(inputPath) ? FileSizeFormatter.Format(new FileInfo(inputPath).Length) : "-";
        CancelCommand = new RelayCommand(Cancel, () => CanCancel);
    }

    public string InputPath { get; }

    public string FileName { get; }

    public MediaInfo? MediaInfo { get; private set; }

    public ICommand CancelCommand { get; }

    public JobState State
    {
        get => _state;
        set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(CanCancel));
                RaiseCancelCanExecuteChanged();
            }
        }
    }

    public string StatusText => State switch
    {
        JobState.Pending => "等待",
        JobState.Probing => "识别中",
        JobState.Ready => "就绪",
        JobState.Running => "处理中",
        JobState.Completed => "完成",
        JobState.Failed => "失败",
        JobState.Skipped => "跳过",
        JobState.Canceled => "已取消",
        _ => State.ToString()
    };

    public double Progress
    {
        get => _progress;
        set
        {
            if (SetProperty(ref _progress, Math.Clamp(value, 0d, 100d)))
            {
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public string ProgressText => $"{Progress:0}%";

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string? OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public string DurationText
    {
        get => _durationText;
        private set => SetProperty(ref _durationText, value);
    }

    public string SizeText
    {
        get => _sizeText;
        private set => SetProperty(ref _sizeText, value);
    }

    public string CodecText
    {
        get => _codecText;
        private set => SetProperty(ref _codecText, value);
    }

    public bool CanCancel => State == JobState.Running && _cancellationTokenSource is not null;

    public bool CanProcess => State is JobState.Pending or JobState.Ready or JobState.Failed;

    public void ApplyMediaInfo(MediaInfo mediaInfo)
    {
        MediaInfo = mediaInfo;
        DurationText = mediaInfo.Duration is null ? "-" : FormatDuration(mediaInfo.Duration.Value);
        SizeText = mediaInfo.FileSizeBytes is null ? SizeText : FileSizeFormatter.Format(mediaInfo.FileSizeBytes.Value);
        CodecText = string.IsNullOrWhiteSpace(mediaInfo.PrimaryCodec) ? "-" : mediaInfo.PrimaryCodec;
    }

    public void PrepareForRun()
    {
        State = JobState.Running;
        Progress = 0;
        Message = "正在处理";
    }

    public void MarkReady()
    {
        State = JobState.Ready;
        Message = "已识别";
    }

    public void MarkFailed(string message)
    {
        State = JobState.Failed;
        Message = message;
    }

    public void MarkCompleted(string outputPath)
    {
        OutputPath = outputPath;
        State = JobState.Completed;
        Progress = 100;
        Message = "已完成";
    }

    public void MarkSkipped(string message)
    {
        State = JobState.Skipped;
        Message = message;
    }

    public void MarkCanceled()
    {
        State = JobState.Canceled;
        Message = "用户取消";
    }

    public void ResetForRetry()
    {
        if (State == JobState.Failed)
        {
            State = JobState.Pending;
            Progress = 0;
            Message = "等待重试";
        }
    }

    public void AttachCancellation(CancellationTokenSource cancellationTokenSource)
    {
        _cancellationTokenSource = cancellationTokenSource;
        OnPropertyChanged(nameof(CanCancel));
        RaiseCancelCanExecuteChanged();
    }

    public void DetachCancellation()
    {
        _cancellationTokenSource = null;
        OnPropertyChanged(nameof(CanCancel));
        RaiseCancelCanExecuteChanged();
    }

    public void UpdateProgress(double percentage, string? message = null)
    {
        Progress = percentage;
        if (!string.IsNullOrWhiteSpace(message))
        {
            Message = message;
        }
    }

    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    private void RaiseCancelCanExecuteChanged()
    {
        if (CancelCommand is RelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
        }
    }

    private static string FormatDuration(TimeSpan duration)
        => duration.TotalHours >= 1 ? duration.ToString(@"h\:mm\:ss") : duration.ToString(@"m\:ss");
}
