<div align=center>
   <img width="512" height="512" alt="STREAMFORGE LOGO" src="https://github.com/user-attachments/assets/9ffb9f1b-2aac-4c64-ab23-bdfe624ccb7e" />
</div>

## StreamForge
A Windows desktop application for screen and window capture with a Discord-style UI.

## Features

- **Display Capture**: DXGI Desktop Duplication API for high-performance screen capture
- **Window Capture**: GDI-based window capture using PrintWindow API
- **WebRTC Streaming**: Efficient peer-to-peer video streaming using MixedReality.WebRTC
- **Modern Discord-Style UI**: Beautiful dark theme with ModernWpf controls
- **Sidebar Navigation**: Quick access to all features with status indicators
- **Live Preview**: Real-time preview of captured content with resolution display
- **Stream Viewer**: Built-in viewer with professional status overlay
- **Multi-Display Support**: Capture from any connected display
- **Smart Window Filtering**: Automatically filters out small and hidden windows
- **Window Thumbnails**: Live preview thumbnails with hover effects (updates every 2 seconds)

## Requirements

- Windows 10/11
- .NET 8.0 SDK
- Node.js 16+ (for WebSocket server)

## Installation

1. Clone the repository
2. Install .NET dependencies:
   ```bash
   dotnet restore
   ```
3. Install Node.js dependencies:
   ```bash
   cd server
   npm install
   cd ..
   ```

## Quick Start

**New user?** See [GETTING_STARTED.md](GETTING_STARTED.md) for a quick introduction!

### Fast Setup
```bash
# 1. Install dependencies
dotnet restore && cd server && npm install && cd ..

# 2. Start signaling server
node server/signaling-server.js

# 3. Run application
dotnet run
```

## Usage

### Sharing Your Screen

1. Click "Share Screen" button
2. Select a display or window from the Discord-style selection UI
3. Your screen is now streaming via WebRTC
4. Click "Stop Stream" to stop

### Viewing a Stream

1. Click "View Stream" button
2. Click "Connect" (default: ws://localhost:8080)
3. You'll see the live stream

### HTML Viewers

- **Local**: `viewers/viewer.html` (localhost:8080)
- **Remote**: `viewers/viewer-remote.html` (custom URL)

### Remote Access

See [docs/VSCODE_SETUP.md](docs/VSCODE_SETUP.md) for VSCode port forwarding or [docs/NETWORK_SETUP.md](docs/NETWORK_SETUP.md) for local network setup.

## Project Structure

```
StreamForge/
├── src/                           # .NET Application Source
│   ├── Core/                      # Core Logic
│   │   ├── ScreenCapture.cs       # DXGI/GDI capture implementation
│   │   └── WebRTCClient.cs        # WebSocket client with compression
│   ├── UI/                        # User Interface
│   │   ├── MainWindow.xaml/cs     # Main window with live preview
│   │   ├── SelectionWindow.xaml/cs # Discord-style selection UI
│   │   └── ViewerWindow.xaml/cs   # Stream viewer window
│   ├── App.xaml/cs                # Application entry point
│   └── AssemblyInfo.cs            # Assembly metadata
├── server/                        # Node.js WebSocket Server
│   ├── signaling-server.js        # WebSocket broadcast server
│   ├── package.json               # Node.js dependencies
│   └── package-lock.json          # Dependency lock file
├── viewers/                       # HTML Viewers
│   ├── viewer.html                # Local viewer (localhost)
│   └── viewer-remote.html         # Remote viewer (configurable URL)
├── docs/                          # Documentation
│   ├── TROUBLESHOOTING.md         # Problem solving guide
│   ├── WEBRTC_MIGRATION.md        # Technical implementation
│   ├── PERFORMANCE_COMPARISON.md  # Performance metrics
│   ├── VSCODE_SETUP.md            # VSCode port forwarding
│   └── NETWORK_SETUP.md           # Network configuration
├── ScreenShareApp.csproj          # .NET application project
├── ScreenShareApp.sln             # Visual Studio solution
├── GETTING_STARTED.md             # Quick start guide
├── STRUCTURE.md                   # Project structure guide
├── .gitignore                     # Git ignore rules
└── README.md                      # This file
```

**See [STRUCTURE.md](STRUCTURE.md) for detailed structure documentation**

## Technologies

### Desktop Application
- WPF (.NET 8.0) - UI framework
- ModernWpf - Modern UI controls and theming
- SharpDX.Direct3D11 & SharpDX.DXGI - DirectX wrapper
- DXGI Desktop Duplication API - High-performance display capture
- GDI+ PrintWindow API - Window capture
- Microsoft.MixedReality.WebRTC - Peer-to-peer video streaming

### Server
- Node.js - WebRTC signaling server runtime
- ws library - WebSocket signaling implementation

## Architecture

### WebRTC Streaming Flow
```
Capture (DXGI/GDI) → WebRTC Encode → P2P Connection → Decode → Display
         ↓                                ↑
         └──────── Signaling Server ──────┘
              (Setup only, no video data)
```

### Performance
- **CPU**: 5-10% (60-70% reduction vs WebSocket)
- **Bandwidth**: 20-80 Mbps adaptive (50-75% reduction)
- **Latency**: 50-150ms (50% reduction)
- **Server Load**: Minimal (95% reduction)

See [docs/PERFORMANCE_COMPARISON.md](docs/PERFORMANCE_COMPARISON.md) for detailed metrics.

### Key Features
- Hardware-accelerated encoding (when available)
- Adaptive bitrate based on network conditions
- Peer-to-peer connection (no server bottleneck)
- Automatic codec negotiation
- Memory-efficient frame handling

## Troubleshooting

### Missing mrwebrtc.dll
- Project configured with `RuntimeIdentifier=win-x64`
- DLL automatically copied during build
- If issue persists: `dotnet clean && dotnet build`
- See [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) for details

### Connection issues
- Ensure signaling server is running
- Check firewall allows port 8080
- Verify STUN server is reachable

### Poor performance
- Verify hardware encoding is available
- Update graphics drivers
- WebRTC adapts automatically to network

For detailed troubleshooting, see [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)

## Known Limitations

- Windows 10/11 only (DXGI Desktop Duplication requirement)
- Some windows may not capture correctly with GDI (use display capture)
- WebRTC may require TURN server for restrictive NAT environments
- Current implementation supports single viewer (can be extended)

## Documentation

- **[GETTING_STARTED.md](GETTING_STARTED.md)** - Quick start guide
- **[STRUCTURE.md](STRUCTURE.md)** - Project structure details
- **[docs/UI_MODERNIZATION.md](docs/UI_MODERNIZATION.md)** - Modern UI features and customization
- **[docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)** - Problem solving
- **[docs/WEBRTC_MIGRATION.md](docs/WEBRTC_MIGRATION.md)** - Technical details
- **[docs/PERFORMANCE_COMPARISON.md](docs/PERFORMANCE_COMPARISON.md)** - Performance metrics
- **[docs/VSCODE_SETUP.md](docs/VSCODE_SETUP.md)** - Remote access setup
- **[docs/NETWORK_SETUP.md](docs/NETWORK_SETUP.md)** - Network configuration

## Contributing

Contributions are welcome! Please ensure:
- Code follows existing style and patterns
- Memory management best practices are maintained
- Changes are tested on Windows 10 and 11
- Documentation is updated for new features

## License

MIT License - See LICENSE file for details
