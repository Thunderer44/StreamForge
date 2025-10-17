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

**📝 See [VSCODE_SETUP.md](VSCODE_SETUP.md) for complete step-by-step guide**

**Quick Summary:**
1. Forward port 8080 (WebSocket) and 5500 (Live Server) in VSCode
2. Set both to **Public** visibility
3. Use `wss://` (not `ws://`) for HTTPS forwarded URLs
4. Access viewer from any device with internet

**Advantages:** No router config, works anywhere, automatic encryption

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
