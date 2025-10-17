# WebRTC Migration Guide

## Overview

StreamForge has been migrated from WebSocket-based streaming to **MixedReality.WebRTC** for more efficient peer-to-peer video streaming. This significantly reduces CPU usage and eliminates server bandwidth bottlenecks.

## What Changed

### Before (WebSocket)
- Captured frames were compressed with GZip
- Sent via WebSocket to signaling server
- Server broadcast frames to all viewers
- High CPU usage for compression
- High bandwidth usage on server
- Manual frame encoding/decoding

### After (WebRTC)
- Frames sent to WebRTC video track source
- Peer-to-peer connection between streamer and viewer
- Hardware-accelerated video encoding (when available)
- Adaptive bitrate based on network conditions
- Minimal server load (only signaling)
- Native WebRTC video pipeline

## Performance Benefits

1. **Lower CPU Usage**: Hardware encoding offloads work from CPU
2. **Better Quality**: Adaptive bitrate maintains quality based on network
3. **Reduced Latency**: Direct peer-to-peer connection
4. **Scalability**: Server only handles signaling, not video data
5. **Bandwidth Efficiency**: WebRTC's built-in compression is more efficient

## Architecture Changes

### Signaling Server
- Changed from frame broadcast to WebRTC signaling
- Handles SDP offer/answer exchange
- Relays ICE candidates between peers
- No longer processes video data

### Desktop Application
- Uses `ExternalVideoTrackSource` for frame injection
- `PeerConnection` manages WebRTC connection
- Automatic codec negotiation
- Built-in network adaptation

## Usage (No Changes Required)

The user interface and workflow remain identical:

1. Start signaling server: `node server/signaling-server.js`
2. Click "Share Screen" to stream
3. Click "View Stream" to watch
4. Everything else works the same!

## Technical Details

### Frame Flow
```
ScreenCapture (DXGI/GDI)
    ↓
SendFrame(byte[], width, height)
    ↓
ExternalVideoTrackSource callback
    ↓
LocalVideoTrack
    ↓
PeerConnection (WebRTC encoding)
    ↓
Network (peer-to-peer)
    ↓
RemoteVideoTrack
    ↓
Argb32VideoFrameReady event
    ↓
WriteableBitmap (WPF display)
```

### Signaling Messages
- `offer`: Initial connection proposal with SDP
- `answer`: Response to offer with SDP
- `candidate`: ICE candidate for NAT traversal

## Known Limitations

1. **NAT Traversal**: May require TURN server for restrictive networks
2. **Codec Support**: Depends on system codec availability
3. **One-to-One**: Current implementation supports single viewer (can be extended)

## Future Enhancements

- Add TURN server configuration for better NAT traversal
- Support multiple simultaneous viewers
- Add data channel for control messages
- Implement bandwidth controls in UI
- Add codec selection options

## Troubleshooting

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for detailed solutions to common issues.
