using System.ComponentModel;
using System.Windows;
using AudioPress.Services;
using AudioPress.ViewModels;

namespace AudioPress;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(new WindowsDialogService());
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (e.Data.GetDataPresent(DataFormats.FileDrop)
            && e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            await viewModel.AddDroppedPathsAsync(paths).ConfigureAwait(true);
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.IsProcessing)
        {
            MessageBox.Show("队列仍在处理中，请先取消或等待结束。", "AudioPress", MessageBoxButton.OK, MessageBoxImage.Information);
            e.Cancel = true;
            return;
        }

        try
        {
            viewModel.SaveSettingsAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Closing should not fail because settings could not be saved.
        }
    }
}

