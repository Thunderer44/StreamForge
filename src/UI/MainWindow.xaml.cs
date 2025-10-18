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
        UpdateStatus("Ready", "");
    }
    
    private void UpdateStatus(string status, string info)
    {
        StatusText.Text = status;
        InfoText.Text = info;
    }

    private void ViewButton_Click(object sender, RoutedEventArgs e)
    {
        var viewerWindow = new ViewerWindow();
        viewerWindow.Show();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StopButton.IsEnabled = false;
        _capture?.Dispose();
        _webrtc?.Dispose();
        _capture = null;
        _webrtc = null;
        _bitmap = null;
        PreviewImage.Source = null;
        HeaderText.Text = "Welcome to StreamForge";
        StopButton.Visibility = Visibility.Collapsed;
        StopButton.IsEnabled = true;
        WelcomePanel.Visibility = Visibility.Visible;
        UpdateStatus("Ready", "");
    }

    private async void ShareButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var displays = ScreenCapture.GetDisplays();
            var windows = ScreenCapture.GetWindows();
            
            var selectionWindow = new SelectionWindow(displays, windows);
            if (selectionWindow.ShowDialog() != true) return;

            var selectedIndex = selectionWindow.SelectedIndex;
            var displayIndex = selectedIndex < displays.Length ? selectedIndex : 0;
            var windowHandle = selectedIndex >= displays.Length ? windows[selectedIndex - displays.Length].hwnd : IntPtr.Zero;

            WelcomePanel.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
            UpdateStatus("Connecting", "Initializing WebRTC...");
            
            _webrtc = new WebRTCClient();
            await _webrtc.InitializeAsync();
            
            _capture = new ScreenCapture();
            _capture.OnCaptureStopped += () =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    var capture = _capture;
                    var webrtc = _webrtc;
                    _capture = null;
                    _webrtc = null;
                    _bitmap = null;
                    PreviewImage.Source = null;
                    HeaderText.Text = "Welcome to StreamForge";
                    StopButton.Visibility = Visibility.Collapsed;
                    WelcomePanel.Visibility = Visibility.Visible;
                    UpdateStatus("Ready", "");
                    
                    capture?.Dispose();
                    webrtc?.Dispose();
                    
                    MessageBox.Show("Stream stopped - Window was closed", "Stream Stopped", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            };
            _capture.OnFrameCaptured += (data, width, height) =>
            {
                _webrtc?.SendFrame(data, width, height);
                Dispatcher.Invoke(() =>
                {
                    if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
                    {
                        _bitmap = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
                        PreviewImage.Source = _bitmap;
                        UpdateStatus("Streaming", $"Live at {width}x{height}");
                    }
                    HeaderText.Text = $"🔴 Live - {width}x{height}";
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