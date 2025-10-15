# Project Structure Guide

This document explains the organization of the StreamForge project.

## Directory Layout

```
StreamForge/
├── src/                           # .NET Application Source Code
│   ├── Core/                      # Core Business Logic
│   │   ├── ScreenCapture.cs       # Screen/window capture using DXGI & GDI
│   │   └── WebRTCClient.cs        # WebSocket client with GZip compression
│   ├── UI/                        # User Interface Components
│   │   ├── MainWindow.xaml/cs     # Main application window
│   │   ├── SelectionWindow.xaml/cs # Screen/window selection dialog
│   │   └── ViewerWindow.xaml/cs   # Stream viewer window
│   ├── App.xaml/cs                # WPF application entry point
│   └── AssemblyInfo.cs            # Assembly metadata
├── server/                        # Node.js WebSocket Server
│   ├── signaling-server.js        # WebSocket broadcast server
│   ├── package.json               # Node.js dependencies
│   ├── package-lock.json          # Dependency lock file
│   └── README.md                  # Server documentation
├── viewers/                       # HTML Stream Viewers
│   ├── viewer.html                # Local viewer (localhost)
│   ├── viewer-remote.html         # Remote viewer (configurable URL)
│   └── README.md                  # Viewer documentation
├── docs/                          # Project Documentation
│   ├── VSCODE_SETUP.md            # VSCode port forwarding guide
│   └── NETWORK_SETUP.md           # Network configuration guide
├── bin/                           # Build output (generated)
├── obj/                           # Build intermediates (generated)
├── ScreenShareApp.csproj          # .NET project configuration
├── ScreenShareApp.sln             # Visual Studio solution file
├── .gitignore                     # Git ignore rules
├── README.md                      # Main project documentation
└── STRUCTURE.md                   # This file
```

## Component Responsibilities

### src/Core/
Contains the core business logic independent of UI:
- **ScreenCapture.cs**: Handles screen and window capture using DXGI Desktop Duplication API and GDI PrintWindow API
- **WebRTCClient.cs**: Manages WebSocket connection, frame compression, and streaming

### src/UI/
Contains all WPF user interface components:
- **MainWindow**: Main application window with live preview and stream controls
- **SelectionWindow**: Discord-style selection interface for choosing screens/windows
- **ViewerWindow**: Stream viewer window for watching remote streams

### server/
Node.js WebSocket server that broadcasts frames to viewers:
- Listens on port 8080
- Broadcasts binary frame data to all connected clients
- Minimal latency with no frame buffering

### viewers/
HTML-based viewers for displaying the stream:
- **viewer.html**: For local testing (connects to localhost)
- **viewer-remote.html**: For remote testing (configurable server URL)

### docs/
Comprehensive documentation for setup and configuration:
- **VSCODE_SETUP.md**: Step-by-step VSCode port forwarding guide
- **NETWORK_SETUP.md**: Network configuration for various scenarios

## Build Configuration

The project uses a custom build configuration in `ScreenShareApp.csproj`:
- Disables default item inclusion to support custom directory structure
- Explicitly includes files from `src/` directory
- Maintains compatibility with .NET 8.0 WPF SDK

## Development Workflow

1. **Desktop Application**: Work in `src/` directory
   - Core logic in `src/Core/`
   - UI components in `src/UI/`

2. **Server**: Work in `server/` directory
   - Install dependencies: `cd server && npm install`
   - Run server: `node server/signaling-server.js`

3. **Viewers**: Work in `viewers/` directory
   - Open HTML files directly or via Live Server
   - Test locally with `viewer.html`
   - Test remotely with `viewer-remote.html`

4. **Documentation**: Work in `docs/` directory
   - Update guides as features change
   - Keep README.md in sync with project structure

## Benefits of This Structure

1. **Separation of Concerns**: Core logic, UI, server, and viewers are clearly separated
2. **Maintainability**: Easy to locate and modify specific components
3. **Scalability**: Simple to add new features in appropriate directories
4. **Documentation**: Centralized docs with component-specific READMEs
5. **Build Clarity**: Clear distinction between source and generated files

## Adding New Components

### New Core Logic Class
Add to `src/Core/` - automatically included in build

### New UI Window
Add XAML and code-behind to `src/UI/` - automatically included in build

### New Viewer
Add HTML file to `viewers/` with appropriate documentation

### New Documentation
Add markdown file to `docs/` and reference in main README.md
