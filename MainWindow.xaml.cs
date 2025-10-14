using System.Windows;

namespace ScreenShareApp;

public partial class MainWindow : Window
{
    private ScreenCapture? _capture;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StopButton.IsEnabled = false;
        await System.Threading.Tasks.Task.Run(() => _capture?.Dispose());
        _capture = null;
        PreviewImage.Source = null;
        Title = "MainWindow";
        StopButton.Visibility = Visibility.Collapsed;
        StopButton.IsEnabled = true;
        ShareButton.Visibility = Visibility.Visible;
    }

    private void ShareButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var displays = ScreenCapture.GetDisplays();
            var windows = ScreenCapture.GetWindows();
            var windowHandles = windows.Select(w => w.hwnd).ToArray();
            var windowNames = windows.Select(w => w.name).ToArray();
            
            var selectionWindow = new SelectionWindow(displays, windowNames);
            if (selectionWindow.ShowDialog() != true) return;

            var selectedIndex = selectionWindow.SelectedIndex;
            var displayIndex = selectedIndex < displays.Length ? selectedIndex : 0;
            var windowHandle = selectedIndex >= displays.Length ? windowHandles[selectedIndex - displays.Length] : IntPtr.Zero;

            ShareButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
            _capture = new ScreenCapture();
            _capture.OnFrameCaptured += (data, width, height) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Title = $"Capturing: {width}x{height}";
                    var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
                    bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), data, width * 4, 0);
                    PreviewImage.Source = bitmap;
                });
            };
            _capture.StartCapture(displayIndex, windowHandle);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}\n{ex.StackTrace}", "Capture Error");
        }
    }
}