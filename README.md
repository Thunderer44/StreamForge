# Screen Share App

A Windows desktop application for screen and window capture with a Discord-style UI.

## Features

- **Display Capture**: DXGI Desktop Duplication API for high-performance screen capture
- **Window Capture**: GDI-based window capture using PrintWindow API
- **Discord-Style UI**: Tabbed selection interface for screens and windows
- **Live Preview**: Real-time preview of captured content
- **Multi-Display Support**: Capture from any connected display
- **Smart Window Filtering**: Automatically filters out small and hidden windows

## Requirements

- Windows 10/11
- .NET 8.0 SDK
- Node.js (for signaling server)

## Installation

1. Clone the repository
2. Install dependencies:
   ```bash
   dotnet restore
   npm install
   ```

## Usage

1. Start the application:
   ```bash
   dotnet run
   ```

2. Click "Share Screen" button
3. Select a display or window from the Discord-style selection UI
4. View live preview in the main window
5. Click "Stop Stream" to stop capturing

## Project Structure

- `MainWindow.xaml/cs` - Main application window with preview
- `SelectionWindow.xaml/cs` - Discord-themed selection UI
- `ScreenCapture.cs` - Core capture logic (DXGI + GDI)
- `signaling-server.js` - WebRTC signaling server (Node.js)

## Technologies

- WPF (.NET 8.0)
- SharpDX (DirectX wrapper)
- DXGI Desktop Duplication API
- GDI+ PrintWindow API
