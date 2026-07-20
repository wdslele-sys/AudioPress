using System.IO;
using Microsoft.Win32;

namespace AudioPress.Services;

public sealed class WindowsDialogService : IDialogService
{
    private const string AudioFilter = "音频文件|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.ogg;*.opus;*.wma;*.aiff;*.aif;*.ac3;*.mka;*.ape;*.alac|所有文件|*.*";
    private const string MediaFilter = "音频和视频文件|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.ogg;*.opus;*.wma;*.aiff;*.aif;*.ac3;*.mka;*.ape;*.alac;*.mp4;*.mkv;*.mov;*.avi;*.wmv;*.webm;*.flv;*.f4v;*.m4v;*.ts;*.trp;*.tp;*.mts;*.m2ts;*.3gp;*.3g2;*.mpeg;*.mpg;*.vob;*.ogv;*.asf;*.rm;*.rmvb;*.mxf;*.dv;*.divx;*.mod;*.tod;*.dat;*.nut;*.y4m;*.ivf;*.264;*.h264;*.265;*.h265;*.hevc;*.av1|所有文件|*.*";

    public IReadOnlyList<string> PickMediaFiles(bool includeVideo)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = includeVideo ? "选择音频或视频文件" : "选择音频文件",
            Filter = includeVideo ? MediaFilter : AudioFilter,
            Multiselect = true,
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }

    public string? PickFolder(string? initialDirectory = null)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择文件夹",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            SelectedPath = Directory.Exists(initialDirectory) ? initialDirectory : string.Empty
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }
}
