# Troubleshooting Guide

## Missing mrwebrtc.dll Error

### Problem
When running the application, you get an error about `mrwebrtc.dll` missing or not found.

### Cause
.NET 8.0+ no longer includes version-specific runtime dependencies by default. The native WebRTC DLL needs to be explicitly copied to the output directory.

### Solution
This has been fixed in the project configuration. The `.csproj` file now includes:
- `RuntimeIdentifier` set to `win-x64`
- Custom build target to copy native DLL

If you still encounter this issue:

1. **Clean and rebuild**:
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Verify DLL exists**:
   ```bash
   dir bin\Debug\net8.0-windows\win-x64\mrwebrtc.dll
   ```

3. **Manual copy** (if needed):
   ```bash
   copy "%USERPROFILE%\.nuget\packages\microsoft.mixedreality.webrtc\2.0.2\runtimes\win10-x64\native\mrwebrtc.dll" "bin\Debug\net8.0-windows\win-x64\"
   ```

## Other Common Issues

### Connection Failed
**Symptoms**: Viewer shows "Connection failed" error

**Solutions**:
- Ensure signaling server is running: `node server/signaling-server.js`
- Check firewall allows port 8080
- Verify network connectivity

### Waiting for Stream Forever
**Symptoms**: Viewer stuck on "Waiting for stream..."

**Solutions**:
- Ensure streamer clicked "Share Screen"
- Both peers must connect to same signaling server
- Check STUN server is reachable (stun.l.google.com:19302)
- Review console for WebRTC errors

### Poor Video Quality
**Symptoms**: Blurry or pixelated video

**Solutions**:
- WebRTC adapts to network automatically
- Check network bandwidth
- Verify hardware encoding is working
- Update graphics drivers

### High CPU Usage
**Symptoms**: CPU usage higher than expected

**Solutions**:
- Verify hardware encoding is available (check GPU drivers)
- Software encoding uses more CPU
- Reduce capture resolution
- Close other applications

### Black Screen
**Symptoms**: Viewer shows black screen

**Solutions**:
- Check if window/display is actually visible
- Try different capture source
- Restart both streamer and viewer
- Check console for errors

### Application Crashes on Start
**Symptoms**: App crashes immediately

**Solutions**:
- Verify all dependencies installed: `dotnet restore`
- Check .NET 8.0 SDK is installed
- Ensure mrwebrtc.dll is present
- Run from command line to see error messages

## Debug Mode

Run with verbose logging:
```bash
$env:WEBRTC_TRACE="1"
dotnet run
```

Check output for detailed WebRTC logs.

## Getting Help

1. Check console output for error messages
2. Verify all dependencies are installed
3. Test with simple display capture first
4. Review [GETTING_STARTED.md](../GETTING_STARTED.md)
5. Check [WEBRTC_MIGRATION.md](WEBRTC_MIGRATION.md) for technical details
