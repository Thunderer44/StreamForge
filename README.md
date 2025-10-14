<div align=center>
   <img width="512" height="512" alt="STREAMFORGE LOGO" src="https://github.com/user-attachments/assets/9ffb9f1b-2aac-4c64-ab23-bdfe624ccb7e" />
</div>

## StreamForge
A Windows desktop application for screen and window capture with a Discord-style UI.

## Features

- **Display Capture**: DXGI Desktop Duplication API for high-performance screen capture
- **Window Capture**: GDI-based window capture using PrintWindow API
- **WebSocket Streaming**: Real-time frame streaming via WebSocket with GZip compression
- **Discord-Style UI**: Tabbed selection interface for screens and windows
- **Live Preview**: Real-time preview of captured content
- **Multi-Display Support**: Capture from any connected display
- **Smart Window Filtering**: Automatically filters out small and hidden windows

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
   npm install
   ```

## Usage

### Local Testing

1. Start the WebSocket server:
   ```bash
   node signaling-server.js
   ```

2. Open `viewer.html` in a web browser

3. Start the application:
   ```bash
   dotnet run
   ```

4. Click "Share Screen" button
5. Select a display or window from the Discord-style selection UI
6. View live preview in the main window and browser
7. Click "Stop Stream" to stop capturing

### Remote Testing

For testing on mobile devices or remote access, you have multiple options:

**Option 1: VSCode Port Forwarding (Easiest)**
1. Forward ports 8080 and 5500 in VSCode (set to Public)
2. Open `viewer-remote.html` via Live Server
3. Access from any device using the forwarded URLs

**See [VSCODE_SETUP.md](VSCODE_SETUP.md) for step-by-step instructions**

**Option 2: Local Network or Router Port Forwarding**

**See [NETWORK_SETUP.md](NETWORK_SETUP.md) for detailed configuration**

## Project Structure

### Application Files
- `MainWindow.xaml/cs` - Main application window with live preview
- `SelectionWindow.xaml/cs` - Discord-themed selection UI
- `ScreenCapture.cs` - Core capture logic (DXGI Desktop Duplication + GDI PrintWindow)
- `WebRTCClient.cs` - WebSocket client with GZip compression and frame skipping
- `ScreenShareApp.csproj` - .NET 8.0 WPF project configuration

### Server & Viewers
- `signaling-server.js` - WebSocket broadcast server (Node.js)
- `viewer.html` - Local browser viewer (localhost testing)
- `viewer-remote.html` - Remote browser viewer with configurable WebSocket URL
- `package.json` - Node.js dependencies (ws library)

### Documentation
- `README.md` - Project overview and quick start guide
- `VSCODE_SETUP.md` - VSCode port forwarding setup guide
- `NETWORK_SETUP.md` - Comprehensive network configuration guide

## Technologies

### Desktop Application
- WPF (.NET 8.0) - UI framework
- SharpDX.Direct3D11 & SharpDX.DXGI - DirectX wrapper
- DXGI Desktop Duplication API - High-performance display capture
- GDI+ PrintWindow API - Window capture
- System.Net.WebSockets - WebSocket client
- System.IO.Compression - GZip compression

### Server & Viewer
- Node.js - WebSocket server runtime
- ws library - WebSocket server implementation
- pako.js - GZip decompression in browser
- HTML5 Canvas - Frame rendering

## Architecture

### Streaming Flow
1. **Capture**: DXGI/GDI captures screen/window at 30fps
2. **Compress**: GZip compression (50-90% bandwidth reduction)
3. **Send**: WebSocket binary frames with 4-byte header (width/height)
4. **Broadcast**: Server broadcasts to all connected viewers
5. **Decompress**: Browser decompresses with pako.js
6. **Render**: Canvas displays BGRA frames converted to RGBA

### Memory Management
- Reuses single WriteableBitmap instance for preview
- Reuses single byte buffer for frame data
- No per-frame allocations (prevents memory leaks)
- Stable memory usage over extended sessions

### Performance Features
- Frame skipping prevents WebSocket queue buildup
- Viewer holds last frame instead of showing black frames
- 30fps capture limit reduces CPU usage
- GZip CompressionLevel.Fastest balances speed and size

## Troubleshooting

### Application won't start
- Ensure .NET 8.0 SDK is installed
- Run `dotnet restore` to restore NuGet packages
- Check for SharpDX dependency issues

### No stream in viewer
- Verify WebSocket server is running (`node signaling-server.js`)
- Check browser console (F12) for connection errors
- Ensure "Share Screen" button was clicked in app
- Verify firewall isn't blocking port 8080

### Memory issues
- This version fixes previous memory leaks
- Memory usage should remain stable during streaming
- If issues persist, restart the application

### Poor performance
- Lower resolution displays use less bandwidth
- Close unnecessary applications
- For remote testing, network speed matters
- VSCode port forwarding adds 100-500ms latency

## Known Limitations

- Windows 10/11 only (DXGI Desktop Duplication requirement)
- Some windows may not capture correctly with GDI (use display capture)
- VSCode port forwarding introduces latency for remote testing
- Compression CPU usage scales with resolution

## Contributing

Contributions are welcome! Please ensure:
- Code follows existing style and patterns
- Memory management best practices are maintained
- Changes are tested on Windows 10 and 11
- Documentation is updated for new features

## License

MIT License - See LICENSE file for details
