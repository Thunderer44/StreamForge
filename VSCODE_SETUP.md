# VSCode Port Forwarding Quick Setup

## Step-by-Step Guide

### 1. Start the WebSocket Server

```bash
node signaling-server.js
```

### 2. Forward Port 8080 (WebSocket Server)

1. In VSCode, open the **Ports** panel (View → Ports, or Ctrl+Shift+P → "View: Focus on Ports View")
2. Click **"Forward a Port"** or press the `+` button
3. Enter `8080`
4. Right-click the new port → **Port Visibility** → **Public**
5. Copy the forwarded address (e.g., `https://xxxx-8080.app.github.dev`)

### 3. Start Live Server for Viewer

1. Install "Live Server" extension if not installed
2. Right-click `viewer-remote.html` → **Open with Live Server**
3. This will start on port `5500`

### 4. Forward Port 5500 (Live Server)

1. In the **Ports** panel, click **"Forward a Port"**
2. Enter `5500`
3. Right-click the new port → **Port Visibility** → **Public**
4. Copy the forwarded address (e.g., `https://xxxx-5500.app.github.dev`)

### 5. Access from Remote Device

1. **On your phone/tablet**, open browser and go to:
   ```
   https://xxxx-5500.app.github.dev/viewer-remote.html
   ```

2. **Convert the WebSocket URL** from HTTPS to WSS:
   - Port 8080 forwarded URL: `https://xxxx-8080.app.github.dev`
   - WebSocket URL to enter: `wss://xxxx-8080.app.github.dev`

3. **Enter the WSS URL** in the input field and click **Connect**

4. **Start streaming** from the desktop app

## Example

If your forwarded ports are:
- Port 8080: `https://abc123-8080.app.github.dev`
- Port 5500: `https://abc123-5500.app.github.dev`

Then:
1. Open: `https://abc123-5500.app.github.dev/viewer-remote.html`
2. Enter: `wss://abc123-8080.app.github.dev`
3. Click Connect

## Troubleshooting

### "Connection Failed" or "WebSocket Error"

**Check Port Visibility:**
- Both ports MUST be set to **Public** (not Private)
- Right-click port → Port Visibility → Public

**Verify URLs:**
- Viewer URL uses `https://`
- WebSocket URL uses `wss://` (not `ws://`)

**Check Server:**
- Ensure `node signaling-server.js` is running
- Check terminal for "Streaming server running" message

### "Mixed Content" Error in Browser

- This happens if you use `ws://` instead of `wss://`
- Always use `wss://` for HTTPS pages

### Stream Not Showing

1. Verify desktop app is running and streaming
2. Check browser console (F12) for errors
3. Ensure you clicked "Share Screen" in the desktop app
4. Try refreshing the viewer page

### Slow/Laggy Stream

- VSCode port forwarding adds latency
- For better performance, use local network (see NETWORK_SETUP.md)
- Compression helps but internet speed matters

## Quick Reference

| Component | Port | Protocol | Visibility |
|-----------|------|----------|------------|
| WebSocket Server | 8080 | WSS | Public |
| Live Server | 5500 | HTTPS | Public |

**URL Format:**
- Viewer: `https://[forwarded-5500-url]/viewer-remote.html`
- WebSocket: `wss://[forwarded-8080-url]`
