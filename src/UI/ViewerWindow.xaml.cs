using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenShareApp
{
    public partial class ViewerWindow : Window
    {
        private ClientWebSocket? _ws;
        private CancellationTokenSource? _cts;
        private WriteableBitmap? _bitmap;
        private bool _isConnected;

        public ViewerWindow()
        {
            InitializeComponent();
            Closed += (s, e) => Disconnect();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                Disconnect();
                return;
            }

            try
            {
                ConnectButton.IsEnabled = false;
                ServerUrlBox.IsEnabled = false;
                StatusText.Text = "Connecting...";

                _ws = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                
                await _ws.ConnectAsync(new Uri(ServerUrlBox.Text), _cts.Token);
                
                _isConnected = true;
                ConnectButton.Content = "Disconnect";
                ConnectButton.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ED4245"));
                StatusText.Text = "Waiting for stream...";
                
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Connection failed: {ex.Message}";
                ServerUrlBox.IsEnabled = true;
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }

        private DateTime _lastFrameTime;
        
        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024 * 1024 * 10]; // 10MB buffer
            _lastFrameTime = DateTime.Now;
            
            _ = Task.Run(async () =>
            {
                while (_isConnected)
                {
                    await Task.Delay(1000);
                    if (_isConnected && (DateTime.Now - _lastFrameTime).TotalSeconds > 3)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (_isConnected)
                            {
                                ShowDisconnectedFrame();
                                StatusText.Text = "Stream ended - No frames received";
                            }
                        });
                        break;
                    }
                }
            });
            
            try
            {
                while (_isConnected && _ws?.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts!.Token);
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        var data = ms.ToArray();
                        await Dispatcher.InvokeAsync(() => ProcessFrame(data));
                        _lastFrameTime = DateTime.Now;
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await Dispatcher.InvokeAsync(() => ShowDisconnectedFrame());
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => 
                {
                    if (_isConnected)
                    {
                        ShowDisconnectedFrame();
                        StatusText.Text = $"Connection lost: {ex.Message}";
                    }
                });
            }
        }

        private void ProcessFrame(byte[] data)
        {
            try
            {
                if (data.Length < 4) return;

                var width = (data[0] << 8) | data[1];
                var height = (data[2] << 8) | data[3];

                using var compressedStream = new MemoryStream(data, 4, data.Length - 4);
                using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                using var decompressedStream = new MemoryStream();
                
                gzipStream.CopyTo(decompressedStream);
                var frameData = decompressedStream.ToArray();

                if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
                {
                    _bitmap = new WriteableBitmap(width, height, 96, 96, 
                        System.Windows.Media.PixelFormats.Bgra32, null);
                    StreamImage.Source = _bitmap;
                    StatusText.Visibility = Visibility.Collapsed;
                }

                _bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), 
                    frameData, width * 4, 0);
                
                Title = $"Stream Viewer - {width}x{height}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Frame error: {ex.Message}";
            }
        }

        private void ShowDisconnectedFrame()
        {
            if (_bitmap == null) return;
            
            var width = _bitmap.PixelWidth;
            var height = _bitmap.PixelHeight;
            var frame = new byte[width * height * 4];
            
            for (int i = 0; i < frame.Length; i += 4)
            {
                frame[i] = 40; frame[i + 1] = 40; frame[i + 2] = 40; frame[i + 3] = 255;
            }
            
            DrawText(frame, width, height, "STREAM DISCONNECTED", width / 2, height / 2 - 30, 3);
            DrawText(frame, width, height, "Connection Lost", width / 2, height / 2 + 30, 2);
            
            _bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), frame, width * 4, 0);
            Title = "Stream Viewer - Disconnected";
        }
        
        private void DrawText(byte[] frame, int width, int height, string text, int centerX, int centerY, int scale)
        {
            var font = GetSimpleFont();
            int startX = centerX - (text.Length * 6 * scale) / 2;
            
            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToUpper(text[i]);
                if (font.ContainsKey(c))
                {
                    var pattern = font[c];
                    for (int py = 0; py < 7; py++)
                    {
                        for (int px = 0; px < 5; px++)
                        {
                            if ((pattern[py] & (1 << (4 - px))) != 0)
                            {
                                for (int sy = 0; sy < scale; sy++)
                                {
                                    for (int sx = 0; sx < scale; sx++)
                                    {
                                        int x = startX + i * 6 * scale + px * scale + sx;
                                        int y = centerY - 3 * scale + py * scale + sy;
                                        if (x >= 0 && x < width && y >= 0 && y < height)
                                        {
                                            int idx = (y * width + x) * 4;
                                            frame[idx] = 255; frame[idx + 1] = 255; frame[idx + 2] = 255;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private Dictionary<char, byte[]> GetSimpleFont()
        {
            return new Dictionary<char, byte[]>
            {
                {'A', new byte[]{0x0E,0x11,0x11,0x1F,0x11,0x11,0x11}},
                {'B', new byte[]{0x1E,0x11,0x11,0x1E,0x11,0x11,0x1E}},
                {'C', new byte[]{0x0E,0x11,0x10,0x10,0x10,0x11,0x0E}},
                {'D', new byte[]{0x1E,0x11,0x11,0x11,0x11,0x11,0x1E}},
                {'E', new byte[]{0x1F,0x10,0x10,0x1E,0x10,0x10,0x1F}},
                {'F', new byte[]{0x1F,0x10,0x10,0x1E,0x10,0x10,0x10}},
                {'G', new byte[]{0x0E,0x11,0x10,0x17,0x11,0x11,0x0F}},
                {'H', new byte[]{0x11,0x11,0x11,0x1F,0x11,0x11,0x11}},
                {'I', new byte[]{0x0E,0x04,0x04,0x04,0x04,0x04,0x0E}},
                {'L', new byte[]{0x10,0x10,0x10,0x10,0x10,0x10,0x1F}},
                {'M', new byte[]{0x11,0x1B,0x15,0x15,0x11,0x11,0x11}},
                {'N', new byte[]{0x11,0x19,0x15,0x13,0x11,0x11,0x11}},
                {'O', new byte[]{0x0E,0x11,0x11,0x11,0x11,0x11,0x0E}},
                {'P', new byte[]{0x1E,0x11,0x11,0x1E,0x10,0x10,0x10}},
                {'R', new byte[]{0x1E,0x11,0x11,0x1E,0x14,0x12,0x11}},
                {'S', new byte[]{0x0E,0x11,0x10,0x0E,0x01,0x11,0x0E}},
                {'T', new byte[]{0x1F,0x04,0x04,0x04,0x04,0x04,0x04}},
                {'U', new byte[]{0x11,0x11,0x11,0x11,0x11,0x11,0x0E}},
                {'W', new byte[]{0x11,0x11,0x11,0x15,0x15,0x1B,0x11}},
                {'Y', new byte[]{0x11,0x11,0x0A,0x04,0x04,0x04,0x04}},
                {'Z', new byte[]{0x1F,0x01,0x02,0x04,0x08,0x10,0x1F}},
                {' ', new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00}}
            };
        }
        
        private void Disconnect()
        {
            _isConnected = false;
            _cts?.Cancel();
            _ws?.Dispose();
            _ws = null;
            _cts = null;
            _bitmap = null;
            
            StreamImage.Source = null;
            StatusText.Visibility = Visibility.Visible;
            StatusText.Text = "Not connected";
            ConnectButton.Content = "Connect";
            ConnectButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#5865F2"));
            ServerUrlBox.IsEnabled = true;
        }
    }
}
