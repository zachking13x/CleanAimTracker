using System;
using System.Runtime.InteropServices;

namespace CleanAimTracker.Services
{
    public class RawInputService
    {
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RID_INPUT = 0x10000003;
        private const int RIM_TYPEMOUSE = 0;

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