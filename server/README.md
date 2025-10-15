# WebSocket Streaming Server

Node.js WebSocket server that broadcasts screen capture frames to all connected viewers.

## Installation

```bash
npm install
```

## Usage

```bash
node signaling-server.js
```

Server will start on `0.0.0.0:8080` and accept WebSocket connections.

## Features

- Broadcasts binary frame data to all connected clients
- Logs client connections with remote IP addresses
- Handles client disconnections gracefully
- No frame buffering for minimal latency

## Protocol

The server expects binary WebSocket messages with the following format:
- Bytes 0-1: Width (uint16, big-endian)
- Bytes 2-3: Height (uint16, big-endian)
- Bytes 4+: GZip compressed BGRA pixel data

## Dependencies

- `ws` - WebSocket server implementation
