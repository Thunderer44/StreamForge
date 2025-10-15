# Stream Viewers

HTML-based viewers for displaying the screen capture stream.

## Files

### viewer.html
Local viewer for testing on localhost.
- Connects to `ws://localhost:8080`
- Use for local development and testing

**Usage:**
1. Start the WebSocket server
2. Open `viewer.html` in a web browser
3. Start streaming from the desktop application

### viewer-remote.html
Remote viewer with configurable WebSocket server URL.
- Supports both `ws://` (local) and `wss://` (HTTPS) protocols
- Input field for custom server URL
- Use for remote testing on mobile devices or other networks

**Usage:**
1. Start the WebSocket server
2. Open `viewer-remote.html` via Live Server or web server
3. Enter the WebSocket server URL (e.g., `wss://your-server.com`)
4. Click Connect
5. Start streaming from the desktop application

## Features

- GZip decompression using pako.js
- BGRA to RGBA color format conversion
- HTML5 Canvas rendering
- Automatic reconnection on disconnect
- Connection status indicators (remote viewer)

## Requirements

- Modern web browser with WebSocket support
- Internet connection for CDN resources (pako.js)
