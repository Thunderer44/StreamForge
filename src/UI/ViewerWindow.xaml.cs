using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.MixedReality.WebRTC;

namespace ScreenShareApp
{
    public partial class ViewerWindow : Window
    {
        private PeerConnection? _peer;
        private ClientWebSocket? _signaling;
        private CancellationTokenSource? _cts;
        private WriteableBitmap? _bitmap;
        private bool _isConnected;
        private DateTime _lastFrameTime;

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

                _cts = new CancellationTokenSource();
                _peer = new PeerConnection();
                
                var config = new PeerConnectionConfiguration
                {
                    IceServers = new List<IceServer>
                    {
                        new IceServer { Urls = { "stun:stun.l.google.com:19302" } }
                    }
                };
                await _peer.InitializeAsync(config);
                
                _peer.AddTransceiver(MediaKind.Video);
                
                _peer.VideoTrackAdded += (RemoteVideoTrack track) =>
                {
                    track.Argb32VideoFrameReady += (Argb32VideoFrame frame) =>
                    {
                        _lastFrameTime = DateTime.Now;
                        
                        var width = (int)frame.width;
                        var height = (int)frame.height;
                        var stride = (int)frame.stride;
                        var data = new byte[height * stride];
                        
                        unsafe
                        {
                            var ptr = (byte*)frame.data.ToPointer();
                            for (int i = 0; i < data.Length; i++)
                                data[i] = ptr[i];
                        }
                        
                        Dispatcher.BeginInvoke(() => RenderFrame(data, width, height));
                    };
                };
                
                _peer.LocalSdpReadytoSend += async (SdpMessage msg) =>
                {
                    await SendSignalingMessage(new { type = msg.Type.ToString().ToLower(), sdp = msg.Content });
                };
                
                _peer.IceCandidateReadytoSend += async (IceCandidate candidate) =>
                {
                    await SendSignalingMessage(new { type = "candidate", candidate = candidate.Content, sdpMid = candidate.SdpMid, sdpMLineIndex = candidate.SdpMlineIndex });
                };
                
                _signaling = new ClientWebSocket();
                await _signaling.ConnectAsync(new Uri(ServerUrlBox.Text), _cts.Token);
                
                _isConnected = true;
                ConnectButton.Content = "Disconnect";
                ConnectButton.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ED4245"));
                StatusText.Text = "Waiting for stream...";
                
                _lastFrameTime = DateTime.Now;
                _ = ReceiveSignaling();
                _ = MonitorConnection();
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

        private async Task SendSignalingMessage(object message)
        {
            if (_signaling?.State != WebSocketState.Open) return;
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _signaling.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ReceiveSignaling()
        {
            var buffer = new byte[8192];
            while (_isConnected && _signaling?.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _signaling.ReceiveAsync(new ArraySegment<byte>(buffer), _cts!.Token);
                    if (result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var msg = JsonSerializer.Deserialize<JsonElement>(json);
                        var type = msg.GetProperty("type").GetString();
                        
                        if (type == "offer")
                        {
                            await _peer!.SetRemoteDescriptionAsync(new SdpMessage { Type = SdpMessageType.Offer, Content = msg.GetProperty("sdp").GetString()! });
                            _peer.CreateAnswer();
                        }
                        else if (type == "answer")
                        {
                            await _peer!.SetRemoteDescriptionAsync(new SdpMessage { Type = SdpMessageType.Answer, Content = msg.GetProperty("sdp").GetString()! });
                        }
                        else if (type == "candidate")
                        {
                            _peer!.AddIceCandidate(new IceCandidate
                            {
                                Content = msg.GetProperty("candidate").GetString()!,
                                SdpMid = msg.GetProperty("sdpMid").GetString()!,
                                SdpMlineIndex = msg.GetProperty("sdpMLineIndex").GetInt32()
                            });
                        }
                    }
                }
                catch { break; }
            }
        }

        private async Task MonitorConnection()
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
        }

        private void RenderFrame(byte[] data, int width, int height)
        {
            try
            {
                if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
                {
                    _bitmap = new WriteableBitmap(width, height, 96, 96, 
                        System.Windows.Media.PixelFormats.Bgra32, null);
                    StreamImage.Source = _bitmap;
                    StatusText.Visibility = Visibility.Collapsed;
                }

                _bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), 
                    data, width * 4, 0);
                
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
            _peer?.Close();
            _peer?.Dispose();
            _signaling?.Dispose();
            _peer = null;
            _signaling = null;
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
