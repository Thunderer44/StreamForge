using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace ScreenShareApp
{
    public class ScreenCapture : IDisposable
    {
        private Device? _device;
        private OutputDuplication? _duplication;
        private Texture2D? _staging;
        private bool _running;
        private Thread? _thread;
        private IntPtr _windowHandle;

        public event Action<byte[], int, int>? OnFrameCaptured;
        public event Action? OnCaptureStopped;

        public static string[] GetDisplays()
        {
            using var factory = new Factory1();
            using var adapter = factory.Adapters1[0];
            return adapter.Outputs.Select((o, i) => $"Display {i + 1}: {o.Description.DeviceName}").ToArray();
        }

        public static (string name, IntPtr hwnd)[] GetWindows()
        {
            var windows = new System.Collections.Generic.List<(string, IntPtr)>();
            EnumWindows((hwnd, _) =>
            {
                if (IsWindowVisible(hwnd) && GetWindowTextLength(hwnd) > 0)
                {
                    GetWindowRect(hwnd, out var rect);
                    if (rect.Right - rect.Left > 100 && rect.Bottom - rect.Top > 100)
                    {
                        var title = new System.Text.StringBuilder(256);
                        GetWindowText(hwnd, title, 256);
                        if (!title.ToString().Equals("Settings", StringComparison.OrdinalIgnoreCase))
                            windows.Add((title.ToString(), hwnd));
                    }
                }
                return true;
            }, IntPtr.Zero);
            return windows.ToArray();
        }

        public void StartCapture(int displayIndex = 0, IntPtr windowHandle = default)
        {
            _windowHandle = windowHandle;
            
            if (_windowHandle == IntPtr.Zero)
            {
                _device = new Device(SharpDX.Direct3D.DriverType.Hardware);
                using var factory = new Factory1();
                using var adapter = factory.Adapters1[0];
                using var output = adapter.Outputs[displayIndex];
                using var output1 = output.QueryInterface<Output1>();
                _duplication = output1.DuplicateOutput(_device);
            }

            _running = true;
            _thread = new Thread(CaptureLoop) { IsBackground = true };
            _thread.Start();
        }

        private void CaptureLoop()
        {
            if (_windowHandle != IntPtr.Zero)
                CaptureWindowLoop();
            else
                CaptureDisplayLoop();
        }

        private void CaptureWindowLoop()
        {
            byte[]? pauseFrame = null;
            int pauseWidth = 0, pauseHeight = 0;
            
            while (_running)
            {
                try
                {
                    if (!IsWindow(_windowHandle))
                    {
                        _running = false;
                        OnCaptureStopped?.Invoke();
                        break;
                    }
                    
                    if (IsIconic(_windowHandle))
                    {
                        if (pauseFrame != null)
                            OnFrameCaptured?.Invoke(pauseFrame, pauseWidth, pauseHeight);
                        Thread.Sleep(100);
                        continue;
                    }
                    
                    GetClientRect(_windowHandle, out var clientRect);
                    var w = clientRect.Right - clientRect.Left;
                    var h = clientRect.Bottom - clientRect.Top;
                    if (w <= 0 || h <= 0) { Thread.Sleep(100); continue; }

                    var hdcWindow = GetDC(_windowHandle);
                    var hdcMem = CreateCompatibleDC(hdcWindow);
                    var hBitmap = CreateCompatibleBitmap(hdcWindow, w, h);
                    SelectObject(hdcMem, hBitmap);
                    PrintWindow(_windowHandle, hdcMem, 3);

                    var bmi = new BITMAPINFOHEADER 
                    { 
                        biSize = Marshal.SizeOf<BITMAPINFOHEADER>(), 
                        biWidth = w, 
                        biHeight = -h, 
                        biPlanes = 1, 
                        biBitCount = 32 
                    };
                    var bytes = new byte[w * h * 4];
                    GetDIBits(hdcMem, hBitmap, 0, (uint)h, bytes, ref bmi, 0);

                    DeleteObject(hBitmap);
                    DeleteDC(hdcMem);
                    ReleaseDC(_windowHandle, hdcWindow);

                    if (pauseFrame == null || pauseWidth != w || pauseHeight != h)
                    {
                        pauseFrame = CreatePauseFrame(w, h);
                        pauseWidth = w;
                        pauseHeight = h;
                    }

                    OnFrameCaptured?.Invoke(bytes, w, h);
                    Thread.Sleep(33);
                }
                catch { Thread.Sleep(100); }
            }
        }
        
        private byte[] CreatePauseFrame(int width, int height)
        {
            var frame = new byte[width * height * 4];
            for (int i = 0; i < frame.Length; i += 4)
            {
                frame[i] = 40; frame[i + 1] = 40; frame[i + 2] = 40; frame[i + 3] = 255;
            }
            
            DrawText(frame, width, height, "STREAM PAUSED", width / 2, height / 2 - 30, 3);
            DrawText(frame, width, height, "Window Minimized", width / 2, height / 2 + 30, 2);
            
            return frame;
        }
        
        private void DrawText(byte[] frame, int width, int height, string text, int centerX, int centerY, int scale)
        {
            var font = GetSimpleFont();
            int startX = centerX - (text.Length * 6 * scale) / 2;
            
            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToUpper(text[i]);
                if (font.ContainsKey(c))
                {
                    var pattern = font[c];
                    for (int py = 0; py < 7; py++)
                    {
                        for (int px = 0; px < 5; px++)
                        {
                            if ((pattern[py] & (1 << (4 - px))) != 0)
                            {
                                for (int sy = 0; sy < scale; sy++)
                                {
                                    for (int sx = 0; sx < scale; sx++)
                                    {
                                        int x = startX + i * 6 * scale + px * scale + sx;
                                        int y = centerY - 3 * scale + py * scale + sy;
                                        if (x >= 0 && x < width && y >= 0 && y < height)
                                        {
                                            int idx = (y * width + x) * 4;
                                            frame[idx] = 255; frame[idx + 1] = 255; frame[idx + 2] = 255;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private System.Collections.Generic.Dictionary<char, byte[]> GetSimpleFont()
        {
            return new System.Collections.Generic.Dictionary<char, byte[]>
            {
                {'A', new byte[]{0x0E,0x11,0x11,0x1F,0x11,0x11,0x11}},
                {'B', new byte[]{0x1E,0x11,0x11,0x1E,0x11,0x11,0x1E}},
                {'C', new byte[]{0x0E,0x11,0x10,0x10,0x10,0x11,0x0E}},
                {'D', new byte[]{0x1E,0x11,0x11,0x11,0x11,0x11,0x1E}},
                {'E', new byte[]{0x1F,0x10,0x10,0x1E,0x10,0x10,0x1F}},
                {'F', new byte[]{0x1F,0x10,0x10,0x1E,0x10,0x10,0x10}},
                {'G', new byte[]{0x0E,0x11,0x10,0x17,0x11,0x11,0x0F}},
                {'H', new byte[]{0x11,0x11,0x11,0x1F,0x11,0x11,0x11}},
                {'I', new byte[]{0x0E,0x04,0x04,0x04,0x04,0x04,0x0E}},
                {'L', new byte[]{0x10,0x10,0x10,0x10,0x10,0x10,0x1F}},
                {'M', new byte[]{0x11,0x1B,0x15,0x15,0x11,0x11,0x11}},
                {'N', new byte[]{0x11,0x19,0x15,0x13,0x11,0x11,0x11}},
                {'O', new byte[]{0x0E,0x11,0x11,0x11,0x11,0x11,0x0E}},
                {'P', new byte[]{0x1E,0x11,0x11,0x1E,0x10,0x10,0x10}},
                {'R', new byte[]{0x1E,0x11,0x11,0x1E,0x14,0x12,0x11}},
                {'S', new byte[]{0x0E,0x11,0x10,0x0E,0x01,0x11,0x0E}},
                {'T', new byte[]{0x1F,0x04,0x04,0x04,0x04,0x04,0x04}},
                {'U', new byte[]{0x11,0x11,0x11,0x11,0x11,0x11,0x0E}},
                {'W', new byte[]{0x11,0x11,0x11,0x15,0x15,0x1B,0x11}},
                {'Y', new byte[]{0x11,0x11,0x0A,0x04,0x04,0x04,0x04}},
                {'Z', new byte[]{0x1F,0x01,0x02,0x04,0x08,0x10,0x1F}},
                {' ', new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00}}
            };
        }

        private void CaptureDisplayLoop()
        {
            byte[]? buffer = null;
            while (_running)
            {
                SharpDX.DXGI.Resource? frame = null;
                try
                {
                    var result = _duplication!.TryAcquireNextFrame(100, out _, out frame);
                    if (result.Failure || frame is null)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    using var texture = frame!.QueryInterface<Texture2D>();
                    var desc = texture.Description;

                    if (_staging == null)
                    {
                        desc.CpuAccessFlags = CpuAccessFlags.Read;
                        desc.Usage = ResourceUsage.Staging;
                        desc.BindFlags = BindFlags.None;
                        desc.OptionFlags = ResourceOptionFlags.None;
                        _staging = new Texture2D(_device, desc);
                    }

                    _device!.ImmediateContext.CopyResource(texture, _staging);
                    var box = _device.ImmediateContext.MapSubresource(_staging, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                    
                    var size = desc.Width * desc.Height * 4;
                    if (buffer == null || buffer.Length != size)
                        buffer = new byte[size];
                    
                    if (box.RowPitch == desc.Width * 4)
                    {
                        Utilities.Read(box.DataPointer, buffer, 0, buffer.Length);
                    }
                    else
                    {
                        for (int y = 0; y < desc.Height; y++)
                        {
                            Utilities.Read(box.DataPointer + y * box.RowPitch, buffer, y * desc.Width * 4, desc.Width * 4);
                        }
                    }
                    
                    _device.ImmediateContext.UnmapSubresource(_staging, 0);
                    OnFrameCaptured?.Invoke(buffer, desc.Width, desc.Height);
                    
                    _duplication!.ReleaseFrame();
                    Thread.Sleep(33);
                }
                catch (SharpDXException) { Thread.Sleep(10); }
                finally
                {
                    frame?.Dispose();
                }
            }
        }

        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder text, int count);
        [DllImport("user32.dll")] static extern int GetWindowTextLength(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
        [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr hdcDest, int x, int y, int w, int h, IntPtr hdcSrc, int x1, int y1, uint rop);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint lines, byte[] bits, ref BITMAPINFOHEADER bmi, uint usage);
        
        delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        
        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFOHEADER 
        { 
            public int biSize, biWidth, biHeight; 
            public short biPlanes, biBitCount; 
            public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant; 
        }

        public void Dispose()
        {
            _running = false;
            _thread?.Join();
            _staging?.Dispose();
            _duplication?.Dispose();
            _device?.Dispose();
        }
    }
}
