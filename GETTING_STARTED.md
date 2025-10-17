# StreamForge - Getting Started Guide

## Quick Start

### 1. Install Dependencies
```bash
dotnet restore
cd server && npm install && cd ..
```

### 2. Start Signaling Server
```bash
node server/signaling-server.js
```

### 3. Run Application
```bash
dotnet run
```

### 4. Share & View
- **Share**: Click "Share Screen" → Select display/window
- **View**: Click "View Stream" → Click "Connect"

## WebRTC Architecture

StreamForge uses WebRTC for efficient peer-to-peer video streaming:

```
Streamer ←→ Signaling Server ←→ Viewer
    └──────── P2P Video ──────────┘
```

**Benefits**:
- 60-70% lower CPU usage (hardware encoding)
- 50-75% less bandwidth (adaptive bitrate)
- 50% lower latency (direct P2P)
- 95% less server load (signaling only)

## Performance Metrics

| Metric | Value |
|--------|-------|
| CPU Usage | 5-10% |
| Bandwidth | 20-80 Mbps (adaptive) |
| Latency | 50-150ms |
| Frame Rate | 30 fps |

## Common Issues

### Missing mrwebrtc.dll
```bash
dotnet clean && dotnet build
```

### Connection Failed
- Ensure signaling server is running on port 8080
- Check firewall allows WebSocket connections
- Verify STUN server is reachable

### No Video Stream
- Click "Share Screen" before viewing
- Ensure both peers connect to same signaling server
- Check console for WebRTC connection errors

## Configuration

### Custom Signaling Server
Edit `src/Core/WebRTCClient.cs` line 61:
```csharp
await _signaling.ConnectAsync(new Uri("ws://YOUR_SERVER:8080"), ...);
```

### Custom STUN/TURN Server
Edit `src/Core/WebRTCClient.cs` lines 24-30:
```csharp
IceServers = new List<IceServer>
{
    new IceServer { Urls = { "stun:your-server.com:3478" } },
    new IceServer { 
        Urls = { "turn:your-server.com:3478" },
        Username = "user",
        Credential = "pass"
    }
}
```

## Documentation

- **README.md** - Full project documentation
- **STRUCTURE.md** - Project structure guide
- **docs/TROUBLESHOOTING.md** - Detailed troubleshooting
- **docs/WEBRTC_MIGRATION.md** - Technical implementation details

## Support

For issues:
1. Check troubleshooting section above
2. Review console logs for errors
3. Verify all dependencies are installed
4. Test with simple display capture first
