using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.MixedReality.WebRTC;

namespace ScreenShareApp
{
    public class WebRTCClient : IDisposable
    {
        private PeerConnection? _peer;
        private ExternalVideoTrackSource? _videoSource;
        private LocalVideoTrack? _videoTrack;
        private ClientWebSocket? _signaling;
        private byte[]? _latestFrame;
        private int _latestWidth;
        private int _latestHeight;
        private bool _hasFrame;

        public async Task InitializeAsync()
        {
            _peer = new PeerConnection();
            var config = new PeerConnectionConfiguration
            {
                IceServers = new System.Collections.Generic.List<IceServer>
                {
                    new IceServer { Urls = { "stun:stun.l.google.com:19302" } }
                }
            };
            await _peer.InitializeAsync(config);
            
            _videoSource = ExternalVideoTrackSource.CreateFromArgb32Callback(OnFrameRequested);
            _videoTrack = LocalVideoTrack.CreateFromSource(_videoSource, new LocalVideoTrackInitConfig { trackName = "screen" });
            _videoTrack.Enabled = true;
            
            var transceiver = _peer.AddTransceiver(MediaKind.Video);
            transceiver.LocalVideoTrack = _videoTrack;
            transceiver.DesiredDirection = Transceiver.Direction.SendOnly;
            
            _peer.Connected += () => System.Diagnostics.Debug.WriteLine("✅ Peer connected!");
            _peer.DataChannelAdded += (channel) => System.Diagnostics.Debug.WriteLine("Data channel added");
            
            System.Diagnostics.Debug.WriteLine("Video track created and added to transceiver");
            
            _peer.LocalSdpReadytoSend += async (SdpMessage msg) =>
            {
                System.Diagnostics.Debug.WriteLine($"Sending SDP: {msg.Type}");
                await SendSignalingMessage(new { type = msg.Type.ToString().ToLower(), sdp = msg.Content });
            };
            
            _peer.IceCandidateReadytoSend += async (IceCandidate candidate) =>
            {
                System.Diagnostics.Debug.WriteLine("Sending ICE candidate");
                await SendSignalingMessage(new { type = "candidate", candidate = candidate.Content, sdpMid = candidate.SdpMid, sdpMLineIndex = candidate.SdpMlineIndex });
            };
            
            _signaling = new ClientWebSocket();
            await _signaling.ConnectAsync(new Uri("ws://localhost:8080"), CancellationToken.None);
            System.Diagnostics.Debug.WriteLine($"✅ WebSocket connected: State={_signaling.State}");
            
            // Start receive loop on background thread (fire and forget)
            _ = Task.Run(() => ReceiveSignaling());
            await Task.Delay(100);
            
            _peer.CreateOffer();
        }

        private int _frameCount = 0;
        private int _requestCount = 0;
        public void SendFrame(byte[] frameData, int width, int height)
        {
            lock (this)
            {
                _latestFrame = frameData;
                _latestWidth = width;
                _latestHeight = height;
                _hasFrame = true;
                _frameCount++;
                if (_frameCount % 90 == 0)
                    System.Diagnostics.Debug.WriteLine($"Captured {_frameCount} frames, WebRTC requested {_requestCount}");
            }
        }

        private void OnFrameRequested(in FrameRequest request)
        {
            lock (this)
            {
                _requestCount++;
                if (!_hasFrame || _latestFrame == null)
                {
                    request.CompleteRequest(new Argb32VideoFrame());
                    return;
                }

                unsafe
                {
                    fixed (byte* ptr = _latestFrame)
                    {
                        request.CompleteRequest(new Argb32VideoFrame
                        {
                            data = (IntPtr)ptr,
                            width = (uint)_latestWidth,
                            height = (uint)_latestHeight,
                            stride = _latestWidth * 4
                        });
                    }
                }
            }
        }

        private async Task SendSignalingMessage(object message)
        {
            if (_signaling?.State != WebSocketState.Open) return;
            try
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _signaling.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                System.Diagnostics.Debug.WriteLine($"Sent signaling message: {json.Substring(0, Math.Min(50, json.Length))}...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send signaling: {ex.Message}");
            }
        }

        private async Task ReceiveSignaling()
        {
            try
            {
                var buffer = new byte[8192];
                System.Diagnostics.Debug.WriteLine($"🎧 Listening for signaling messages... Thread={Thread.CurrentThread.ManagedThreadId}");
                System.Diagnostics.Debug.WriteLine($"WebSocket State: {_signaling?.State}");
                
                while (_signaling?.State == WebSocketState.Open)
                {
                    try
                    {
                        var result = await _signaling.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
                        System.Diagnostics.Debug.WriteLine($"✅ Got message! Type={result.MessageType}, Count={result.Count}");
                        
                        if (result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary)
                        {
                            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            System.Diagnostics.Debug.WriteLine($"📩 Received: {json.Substring(0, Math.Min(100, json.Length))}...");
                            var msg = JsonSerializer.Deserialize<JsonElement>(json);
                            var type = msg.GetProperty("type").GetString();
                            
                            if (type == "answer")
                            {
                                System.Diagnostics.Debug.WriteLine("✅ Received answer, setting remote description");
                                await _peer!.SetRemoteDescriptionAsync(new SdpMessage { Type = SdpMessageType.Answer, Content = msg.GetProperty("sdp").GetString()! });
                                System.Diagnostics.Debug.WriteLine("✅ Remote description set!");
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
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Signaling error: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                        break;
                    }
                }
                System.Diagnostics.Debug.WriteLine("❌ Signaling receive loop ended");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ReceiveSignaling crashed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            _videoTrack?.Dispose();
            _videoSource?.Dispose();
            _peer?.Close();
            _peer?.Dispose();
            _signaling?.Dispose();
        }
    }
}
