using System.ComponentModel;
using System.Windows;
using AudioPress.Services;
using AudioPress.ViewModels;

namespace AudioPress;

public partial class MainWindow : Window
{
    private bool _closeInProgress;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(new WindowsDialogService());
    }

    private async void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            && e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] paths)
        {
            await viewModel.AddDroppedPathsAsync(paths).ConfigureAwait(true);
        }
    }

    private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        if (_closeInProgress)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel viewModel)
        {
            _allowClose = true;
            Close();
            return;
        }

        if (viewModel.IsProcessing)
        {
            var result = System.Windows.MessageBox.Show(
                "队列仍在处理中。是否取消当前任务并退出？",
                "AudioPress",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }
        }

        _closeInProgress = true;
        try
        {
            if (viewModel.IsProcessing)
            {
                await viewModel.CancelProcessingAsync()
                    .WaitAsync(TimeSpan.FromSeconds(10))
                    .ConfigureAwait(true);
            }

            await viewModel.SaveSettingsAsync().ConfigureAwait(true);
        }
        catch
        {
            // Closing must still complete if cancellation or settings persistence fails.
        }
        finally
        {
            _allowClose = true;
            Close();
        }
    }
}
