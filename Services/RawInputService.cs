using System;
using System.Runtime.InteropServices;

namespace CleanAimTracker.Services
{
    public class RawInputService
    {
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RID_INPUT = 0x10000003;
        private const int RIM_TYPEMOUSE = 0;
        // -----------------------------
        // Micro-adjustment tracking
        // -----------------------------
        private int microMovementCount = 0;
        private int microJitterCount = 0;
        private double lastMicroAngle = 0;
        private bool lastWasMicro = false;


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

        // Match the native RAWMOUSE layout precisely. The native structure contains a union
        // (ulButtons or { usButtonFlags, usButtonData }) so use Explicit layout with offsets.
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct RAWMOUSE
        {
            [FieldOffset(0)] public ushort usFlags;
            // 2 bytes padding here on native layout before the union

            // Union: start at offset 4
            [FieldOffset(4)] public uint ulButtons;
            [FieldOffset(4)] public ushort usButtonFlags;
            [FieldOffset(6)] public ushort usButtonData;

            [FieldOffset(8)] public uint ulRawButtons;
            [FieldOffset(12)] public int lLastX;
            [FieldOffset(16)] public int lLastY;
            [FieldOffset(20)] public uint ulExtraInformation;
        }

        // Keep RAWINPUT struct definition if needed elsewhere, but avoid marshaling it
        // directly because it contains a union. We'll marshal the header first and
        // then the appropriate payload (mouse/keyboard/hid) at the correct offset.
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            // payload is a union (variable type) - do not rely on this field for marshaling
            // public RAWMOUSE mouse; // intentionally omitted from direct use
        }

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

        public event Action<int, int>? MouseMoved;

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

        public void ProcessRawInput(IntPtr lParam)
        {
            uint dwSize = 0;
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                {
                    // Marshal header first to avoid incorrect union marshaling
                    RAWINPUTHEADER header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);

                    if (header.dwType == RIM_TYPEMOUSE)
                    {
                        // ⭐ Try to detect DPI from HID feature report
                       

                        IntPtr pMouse = IntPtr.Add(buffer, Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                        RAWMOUSE mouse = Marshal.PtrToStructure<RAWMOUSE>(pMouse);

                        int dx = mouse.lLastX;
                        int dy = mouse.lLastY;

                       

                        // Calculate distance for micro-movement detection
                        double distance = Math.Sqrt(dx * dx + dy * dy);

                        // Detect micro-movements (tiny, precise adjustments)
                        bool isMicro = distance < 1.5; // threshold for tiny movement

                        if (isMicro)
                        {
                            microMovementCount++;

                            // Detect jitter (rapid back-and-forth micro corrections)
                            double angle = Math.Atan2(dy, dx);

                            if (lastWasMicro)
                            {
                                double angleDiff = Math.Abs(angle - lastMicroAngle);

                                // If angle flips direction sharply, it's jitter
                                if (angleDiff > Math.PI / 2)
                                    microJitterCount++;
                            }

                            lastMicroAngle = angle;
                            lastWasMicro = true;
                        }
                        else
                        {
                            lastWasMicro = false;
                        }

                        MouseMoved?.Invoke(dx, dy);

                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}