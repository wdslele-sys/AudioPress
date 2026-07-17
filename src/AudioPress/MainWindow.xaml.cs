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

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.IsProcessing)
        {
            System.Windows.MessageBox.Show(
                "队列仍在处理中，请先取消或等待结束。",
                "AudioPress",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
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
