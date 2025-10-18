using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ScreenShareApp
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    
    public class NotNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ShareItem
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public int Index { get; set; }
        public BitmapSource? Preview { get; set; }
        public IntPtr WindowHandle { get; set; }
    }

    public partial class SelectionWindow : Window
    {
        public int SelectedIndex { get; private set; } = -1;
        private readonly string[] _screens;
        private readonly (string name, IntPtr hwnd)[] _windows;
        private DispatcherTimer? _previewTimer;
        private List<ShareItem>? _currentWindowItems;

        public SelectionWindow(string[] screens, (string name, IntPtr hwnd)[] windows)
        {
            InitializeComponent();
            _screens = screens;
            _windows = windows;
            Loaded += (s, e) => ShowScreens();
            Closed += (s, e) => _previewTimer?.Stop();
        }

        private void ShowScreens()
        {
            ScreensTab.Background = (System.Windows.Media.Brush)Application.Current.Resources["DiscordBlurple"];
            ScreensTab.Foreground = System.Windows.Media.Brushes.White;
            WindowsTab.Background = (System.Windows.Media.Brush)Application.Current.Resources["DiscordGray"];
            WindowsTab.Foreground = (System.Windows.Media.Brush)Application.Current.Resources["DiscordMutedText"];
            
            var items = _screens.Select((s, i) => new ShareItem { Name = s, Icon = "🖥", Index = i }).ToList();
            ItemsGrid.ItemsSource = items;
        }

        private void ShowWindows()
        {
            WindowsTab.Background = (System.Windows.Media.Brush)Application.Current.Resources["DiscordBlurple"];
            WindowsTab.Foreground = System.Windows.Media.Brushes.White;
            ScreensTab.Background = (System.Windows.Media.Brush)Application.Current.Resources["DiscordGray"];
            ScreensTab.Foreground = (System.Windows.Media.Brush)Application.Current.Resources["DiscordMutedText"];
            
            _currentWindowItems = _windows.Select((w, i) => new ShareItem 
            { 
                Name = w.name, 
                Icon = "🪟", 
                Index = _screens.Length + i,
                WindowHandle = w.hwnd,
                Preview = CaptureWindowThumbnail(w.hwnd)
            }).ToList();
            ItemsGrid.ItemsSource = _currentWindowItems;
            
            _previewTimer?.Stop();
            _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _previewTimer.Tick += (s, e) => UpdateWindowPreviews();
            _previewTimer.Start();
        }
        
        private void UpdateWindowPreviews()
        {
            if (_currentWindowItems == null) return;
            foreach (var item in _currentWindowItems)
            {
                item.Preview = CaptureWindowThumbnail(item.WindowHandle);
            }
            ItemsGrid.Items.Refresh();
        }
        
        private BitmapSource? CaptureWindowThumbnail(IntPtr hwnd)
        {
            try
            {
                GetClientRect(hwnd, out var rect);
                var w = rect.Right - rect.Left;
                var h = rect.Bottom - rect.Top;
                if (w <= 0 || h <= 0) return null;
                
                var hdcWindow = GetDC(hwnd);
                var hdcMem = CreateCompatibleDC(hdcWindow);
                var hBitmap = CreateCompatibleBitmap(hdcWindow, w, h);
                SelectObject(hdcMem, hBitmap);
                PrintWindow(hwnd, hdcMem, 2);
                
                var bmi = new BITMAPINFOHEADER 
                { 
                    biSize = Marshal.SizeOf<BITMAPINFOHEADER>(), 
                    biWidth = w, 
                    biHeight = -h, 
                    biPlanes = 1, 
                    biBitCount = 32 
                };
                var bytes = new byte[w * h * 4];
                GetDIBits(hdcMem, hBitmap, 0, (uint)h, bytes, ref bmi, 0);
                
                DeleteObject(hBitmap);
                DeleteDC(hdcMem);
                ReleaseDC(hwnd, hdcWindow);
                
                var bitmap = BitmapSource.Create(w, h, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, bytes, w * 4);
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }

        private void ScreensTab_Click(object sender, RoutedEventArgs e)
        {
            _previewTimer?.Stop();
            _currentWindowItems = null;
            ShowScreens();
        }
        
        private void WindowsTab_Click(object sender, RoutedEventArgs e) => ShowWindows();

        private void Item_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int index)
            {
                SelectedIndex = index;
                DialogResult = true;
            }
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedIndex >= 0)
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select a screen or window to share.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint lines, byte[] bits, ref BITMAPINFOHEADER bmi, uint usage);
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        
        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFOHEADER 
        { 
            public int biSize, biWidth, biHeight; 
            public short biPlanes, biBitCount; 
            public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant; 
        }
    }
}
