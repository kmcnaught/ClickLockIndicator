using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClickLockIndicator
{
    public class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;

        public event EventHandler LeftButtonDown;
        public event EventHandler LeftButtonUp;
        public event EventHandler ReleaseTrigger; // R, M, X buttons

        private IntPtr _hookHandle = IntPtr.Zero;
        private NativeMethods.LowLevelMouseProc _proc;

        public MouseHook()
        {
            _proc = HookCallback;
        }

        public void Install()
        {
            using (var module = System.Diagnostics.Process.GetCurrentProcess().MainModule)
                _hookHandle = NativeMethods.SetWindowsHookEx(WH_MOUSE_LL, _proc,
                    NativeMethods.GetModuleHandle(module.ModuleName), 0);
        }

        public void Uninstall()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                switch (msg)
                {
                    case WM_LBUTTONDOWN:
                        LeftButtonDown?.Invoke(this, EventArgs.Empty);
                        break;
                    case WM_LBUTTONUP:
                        LeftButtonUp?.Invoke(this, EventArgs.Empty);
                        break;
                    case WM_RBUTTONDOWN:
                    case WM_RBUTTONUP:
                    case WM_MBUTTONDOWN:
                    case WM_MBUTTONUP:
                    case WM_XBUTTONDOWN:
                    case WM_XBUTTONUP:
                        ReleaseTrigger?.Invoke(this, EventArgs.Empty);
                        break;
                }
            }
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Uninstall();
        }
    }

    internal static class NativeMethods
    {
        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
