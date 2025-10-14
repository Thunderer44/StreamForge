using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;

namespace ScreenShareApp
{
    public class WebRTCClient : IDisposable
    {
        private ClientWebSocket? _ws;
        private bool _isRunning;
        private bool _isSending;

        public async Task InitializeAsync()
        {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(new Uri("ws://localhost:8080"), CancellationToken.None);
            _isRunning = true;
        }

        public Task ConnectSignalingAsync() => Task.CompletedTask;
        public Task CreateOfferAsync() => Task.CompletedTask;

        public async void SendFrame(byte[] frameData, int width, int height)
        {
            if (_ws?.State == WebSocketState.Open && !_isSending)
            {
                _isSending = true;
                try
                {
                    using var ms = new MemoryStream();
                    ms.WriteByte((byte)(width >> 8));
                    ms.WriteByte((byte)width);
                    ms.WriteByte((byte)(height >> 8));
                    ms.WriteByte((byte)height);
                    
                    using (var gzip = new GZipStream(ms, CompressionLevel.Fastest, true))
                    {
                        gzip.Write(frameData, 0, frameData.Length);
                    }
                    
                    await _ws.SendAsync(new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
                catch { }
                finally
                {
                    _isSending = false;
                }
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _ws?.Dispose();
        }
    }
}
