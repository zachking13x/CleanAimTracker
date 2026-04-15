using System;
using System.Runtime.InteropServices;
using CleanAimTracker.Models;

namespace CleanAimTracker.Services
{
    public class RawInputService
    {
        // Active session storage
        private SessionData _currentSession;

        // Raw Input constants
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RID_INPUT = 0x10000003;
        private const int RIM_TYPEMOUSE = 0;

        // -----------------------------
        // Session Control
        // -----------------------------
        public void StartSession()
        {
            _currentSession = new SessionData
            {
                StartTime = DateTime.Now
            };
        }

        public SessionData EndSession()
        {
            if (_currentSession == null)
                return null;

            _currentSession.EndTime = DateTime.Now;

            // ⭐ Build summary
            var summary = _currentSession.BuildSummary();

            // ⭐ Open summary window
            var win = new SummaryWindow(summary);
            win.Activate();

            return _currentSession;
        }

        // -----------------------------
        // Raw Input Structs
        // -----------------------------
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public int dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct RAWMOUSE
        {
            [FieldOffset(0)] public ushort usFlags;

            // Union
            [FieldOffset(4)] public uint ulButtons;
            [FieldOffset(4)] public ushort usButtonFlags;
            [FieldOffset(6)] public ushort usButtonData;

            [FieldOffset(8)] public uint ulRawButtons;
            [FieldOffset(12)] public int lLastX;
            [FieldOffset(16)] public int lLastY;
            [FieldOffset(20)] public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUT
        {
            public RAWINPUTHEADER header;
        }

        // -----------------------------
        // P/Invoke
        // -----------------------------
        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(
            RAWINPUTDEVICE[] pRawInputDevices,
            uint uiNumDevices,
            uint cbSize);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(
            IntPtr hRawInput,
            uint uiCommand,
            IntPtr pData,
            ref uint pcbSize,
            uint cbSizeHeader);

        // Event for UI or other systems
        public event Action<int, int>? MouseMoved;

        // -----------------------------
        // Register Raw Input
        // -----------------------------
        public void Register(nint hwnd)
        {
            RAWINPUTDEVICE[] rid =
            {
                new RAWINPUTDEVICE
                {
                    usUsagePage = 0x01,
                    usUsage = 0x02,
                    dwFlags = RIDEV_INPUTSINK,
                    hwndTarget = (IntPtr)hwnd
                }
            };

            RegisterRawInputDevices(
                rid,
                (uint)rid.Length,
                (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }

        // -----------------------------
        // Process Raw Input
        // -----------------------------
        public void ProcessRawInput(IntPtr lParam)
        {
            uint dwSize = 0;
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                {
                    RAWINPUTHEADER header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);

                    if (header.dwType == RIM_TYPEMOUSE)
                    {
                        IntPtr pMouse = IntPtr.Add(buffer, Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                        RAWMOUSE mouse = Marshal.PtrToStructure<RAWMOUSE>(pMouse);

                        int dx = mouse.lLastX;
                        int dy = mouse.lLastY;

                        // ⭐ RECORD SAMPLE
                        if (_currentSession != null)
                        {
                            _currentSession.Samples.Add(new MouseSample
                            {
                                DeltaX = dx,
                                DeltaY = dy,
                                Velocity = Math.Sqrt(dx * dx + dy * dy),
                                Timestamp = DateTime.Now
                            });
                        }

                        // ⭐ ANALYTICS ENGINE
                        AnalyzeMovement(dx, dy);

                        // Fire event for UI
                        MouseMoved?.Invoke(dx, dy);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // -----------------------------
        // Analytics Engine
        // -----------------------------
        private void AnalyzeMovement(int dx, int dy)
        {
            if (_currentSession == null)
                return;

            double distance = Math.Sqrt(dx * dx + dy * dy);
            double angle = Math.Atan2(dy, dx);

            // Micro-adjustment detection
            if (distance < 1.5)
            {
                _currentSession.MicroAdjustments.Add(new MicroAdjustmentEvent
                {
                    Magnitude = distance,
                    Timestamp = DateTime.Now
                });
            }

            // Overshoot detection
            if (distance > 20)
            {
                _currentSession.Overshoots.Add(new OvershootEvent
                {
                    Amount = distance,
                    Timestamp = DateTime.Now
                });
            }

            // Undershoot detection
            if (distance > 5 && distance <= 20)
            {
                _currentSession.Undershoots.Add(new UndershootEvent
                {
                    Amount = distance,
                    Timestamp = DateTime.Now
                });
            }

            // Flick detection
            if (distance > 30)
            {
                _currentSession.Flicks.Add(new FlickEvent
                {
                    Angle = angle,
                    Distance = distance,
                    Timestamp = DateTime.Now
                });
            }
        }
    }
}
 