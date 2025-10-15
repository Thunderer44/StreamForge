# Network Setup Guide

## Testing on Local Network (Same WiFi)

### Step 1: Find Your Local IP Address

**Windows:**
```bash
ipconfig
```
Look for "IPv4 Address" under your active network adapter (e.g., `192.168.1.100`)

**Alternative:** Run this in PowerShell:
```powershell
(Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.InterfaceAlias -notlike "*Loopback*"}).IPAddress
```

### Step 2: Configure Firewall

**Windows Firewall:**
```bash
# Allow port 8080 inbound
netsh advfirewall firewall add rule name="StreamForge WebSocket" dir=in action=allow protocol=TCP localport=8080
```

Or manually:
1. Open Windows Defender Firewall
2. Click "Advanced settings"
3. Click "Inbound Rules" → "New Rule"
4. Select "Port" → Next
5. Enter port `8080` → Next
6. Allow the connection → Next
7. Apply to all profiles → Next
8. Name it "StreamForge" → Finish

### Step 3: Start the Server

```bash
node signaling-server.js
```

You should see:
```
✅ Streaming server running on ws://0.0.0.0:8080
📱 Access from other devices using your local IP
```

### Step 4: Connect from Other Devices

**On the same network:**
1. Open `viewer-remote.html` on your phone/tablet browser
2. Enter: `ws://YOUR_LOCAL_IP:8080` (e.g., `ws://192.168.1.100:8080`)
3. Click "Connect"
4. Start streaming from the desktop app

## Testing with VSCode Port Forwarding (Easiest)

### Step 1: Forward Ports in VSCode

1. **Forward WebSocket Server (Port 8080):**
   - Open VSCode Command Palette (Ctrl+Shift+P)
   - Type "Forward a Port"
   - Enter `8080`
   - Right-click the forwarded port → Change Port Visibility → **Public**
   - Copy the forwarded URL (e.g., `https://xxxx-xx-xx-xx-xx.ngrok-free.app`)

2. **Forward Live Server (Port 5500):**
   - Forward port `5500`
   - Set visibility to **Public**
   - Copy the forwarded URL

### Step 2: Update WebSocket URL

**Important:** VSCode port forwarding uses HTTPS, so WebSocket must use WSS (secure WebSocket)

- If forwarded URL is: `https://xxxx-8080.app.github.dev`
- WebSocket URL is: `wss://xxxx-8080.app.github.dev`

### Step 3: Access from Any Device

1. Open the Live Server URL on any device: `https://xxxx-5500.app.github.dev/viewer-remote.html`
2. Enter WebSocket URL: `wss://xxxx-8080.app.github.dev` (note: `wss://` not `ws://`)
3. Click "Connect"
4. Start streaming from desktop app

**📝 See [VSCODE_SETUP.md](VSCODE_SETUP.md) for detailed step-by-step guide**

✅ **Advantages:**
- No router configuration needed
- Works from anywhere with internet
- Automatic HTTPS/WSS encryption
- No firewall changes required

## Testing Over Internet (Router Port Forwarding)

### Step 1: Configure Router Port Forwarding

1. Access your router admin panel (usually `192.168.1.1` or `192.168.0.1`)
2. Find "Port Forwarding" or "Virtual Server" settings
3. Add new rule:
   - **External Port:** 8080
   - **Internal Port:** 8080
   - **Internal IP:** Your PC's local IP (from Step 1 above)
   - **Protocol:** TCP

### Step 2: Find Your Public IP

Visit: https://whatismyipaddress.com/

### Step 3: Connect from Remote Device

1. Open `viewer-remote.html` on remote device
2. Enter: `ws://YOUR_PUBLIC_IP:8080`
3. Click "Connect"

## Security Notes

⚠️ **Important:**
- This setup has NO authentication
- Anyone with your IP can view your stream
- Only use on trusted networks
- Close port forwarding when not in use
- Consider using a VPN for remote access instead

## Troubleshooting

**Connection refused:**
- Check firewall settings
- Verify server is running
- Confirm correct IP address

**Can't connect from other devices:**
- Ensure devices are on same network
- Check router firewall settings
- Try disabling Windows Firewall temporarily to test

**Slow/laggy stream:**
- Check network bandwidth
- Reduce capture resolution
- Ensure good WiFi signal
