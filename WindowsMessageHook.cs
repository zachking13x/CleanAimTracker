using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace CleanAimTracker
{
    public static class WindowMessageHook
    {
        private static SubclassProcDelegate _subclassProc;
        private static Func<IntPtr, uint, IntPtr, IntPtr, IntPtr>? _wndProc;

        public static void Initialize(object window, Func<IntPtr, uint, IntPtr, IntPtr, IntPtr> wndProc)
        {
            _wndProc = wndProc;
            _subclassProc = SubclassProc;

            IntPtr hwnd = WindowNative.GetWindowHandle(window);

            if (!SetWindowSubclass(hwnd, _subclassProc, 1, IntPtr.Zero))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static IntPtr SubclassProc(
            IntPtr hwnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam,
            uint uIdSubclass,
            IntPtr dwRefData)
        {
            const uint WM_INPUT = 0x00FF;

            // Forward WM_INPUT to your managed WndProc
            if (msg == WM_INPUT && _wndProc is not null)
            {
                _wndProc(hwnd, msg, wParam, lParam);
            }

            // Always let Windows continue normal processing
            return DefSubclassProc(hwnd, msg, wParam, lParam);
        }

        [DllImport("comctl32.dll", SetLastError = true, EntryPoint = "SetWindowSubclass")]
        private static extern bool SetWindowSubclass(
            IntPtr hWnd,
            SubclassProcDelegate pfnSubclass,
            uint uIdSubclass,
            IntPtr dwRefData);

        [DllImport("comctl32.dll", SetLastError = true, EntryPoint = "DefSubclassProc")]
        private static extern IntPtr DefSubclassProc(
            IntPtr hWnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam);

        private delegate IntPtr SubclassProcDelegate(
            IntPtr hwnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam,
            uint uIdSubclass,
            IntPtr dwRefData);
    }
}