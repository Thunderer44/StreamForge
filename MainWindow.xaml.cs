using System.Windows;

namespace ScreenShareApp;

public partial class MainWindow : Window
{
    private ScreenCapture? _capture;
    private WebRTCClient? _webrtc;
    private System.Windows.Media.Imaging.WriteableBitmap? _bitmap;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StopButton.IsEnabled = false;
        await System.Threading.Tasks.Task.Run(() => 
        {
            _capture?.Dispose();
            _webrtc?.Dispose();
        });
        _capture = null;
        _webrtc = null;
        _bitmap = null;
        PreviewImage.Source = null;
        Title = "MainWindow";
        StopButton.Visibility = Visibility.Collapsed;
        StopButton.IsEnabled = true;
        ShareButton.Visibility = Visibility.Visible;
    }

    private async void ShareButton_Click(object sender, RoutedEventArgs e)
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
            
            _webrtc = new WebRTCClient();
            await _webrtc.InitializeAsync();
            
            _capture = new ScreenCapture();
            _capture.OnFrameCaptured += (data, width, height) =>
            {
                _webrtc?.SendFrame(data, width, height);
                Dispatcher.Invoke(() =>
                {
                    if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
                    {
                        _bitmap = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
                        PreviewImage.Source = _bitmap;
                    }
                    Title = $"Streaming: {width}x{height}";
                    _bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), data, width * 4, 0);
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