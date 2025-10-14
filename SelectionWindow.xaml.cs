using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ScreenShareApp
{
    public class ShareItem
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public int Index { get; set; }
    }

    public partial class SelectionWindow : Window
    {
        public int SelectedIndex { get; private set; } = -1;
        private readonly string[] _screens;
        private readonly string[] _windows;

        public SelectionWindow(string[] screens, string[] windows)
        {
            InitializeComponent();
            _screens = screens;
            _windows = windows;
            Loaded += (s, e) => ShowScreens();
        }

        private void ShowScreens()
        {
            ScreensTab.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#5865F2"));
            ScreensTab.Foreground = System.Windows.Media.Brushes.White;
            WindowsTab.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4F545C"));
            WindowsTab.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#B9BBBE"));
            
            var items = _screens.Select((s, i) => new ShareItem { Name = s, Icon = "🖥", Index = i }).ToList();
            ItemsGrid.ItemsSource = items;
        }

        private void ShowWindows()
        {
            WindowsTab.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#5865F2"));
            WindowsTab.Foreground = System.Windows.Media.Brushes.White;
            ScreensTab.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4F545C"));
            ScreensTab.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#B9BBBE"));
            
            var items = _windows.Select((w, i) => new ShareItem { Name = w, Icon = "🪟", Index = _screens.Length + i }).ToList();
            ItemsGrid.ItemsSource = items;
        }

        private void ScreensTab_Click(object sender, RoutedEventArgs e) => ShowScreens();
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
    }
}
