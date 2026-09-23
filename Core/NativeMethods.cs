using System.Runtime.InteropServices;
using System.Text;

namespace WinTime.Core;

/// <summary>
/// P/Invoke bindings for Win32 API (user32.dll and kernel32.dll).
/// </summary>
internal static class NativeMethods
{
    // Foreground window

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    internal const uint GA_PARENT    = 1;
    internal const uint GA_ROOT      = 2;
    internal const uint GA_ROOTOWNER = 3;

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    // Window enumeration

    internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(IntPtr hWnd);

    internal const int GWL_EXSTYLE = -20;
    internal const long WS_EX_TOOLWINDOW  = 0x00000080L;
    internal const long WS_EX_TRANSPARENT = 0x00000020L;
    internal const long WS_EX_LAYERED     = 0x00080000L;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    internal static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    internal static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    internal static long GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
            return (long)GetWindowLongPtr64(hWnd, nIndex);
        return GetWindowLong32(hWnd, nIndex);
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    internal static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    internal static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    internal static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    // Process info

    internal const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool QueryFullProcessImageName(
        IntPtr hProcess, int dwFlags,
        StringBuilder lpExeName, ref int lpdwSize);

    // Idle / AFK detection

    [StructLayout(LayoutKind.Sequential)]
    internal struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    // Mouse tracking

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(int vKey);

    internal const int VK_LBUTTON  = 0x01;
    internal const int VK_RBUTTON  = 0x02;
    internal const int VK_MBUTTON  = 0x04;
    internal const int VK_XBUTTON1 = 0x05;
    internal const int VK_XBUTTON2 = 0x06;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
    internal static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    internal const uint MB_OK = 0x00000000;
    internal const uint MB_ICONWARNING = 0x00000030;
    internal const uint MB_ICONSTOP = 0x00000010;
    internal const uint MB_TOPMOST = 0x00040000;
    internal const uint MB_SETFOREGROUND = 0x00010000;
    internal const uint MB_SYSTEMMODAL = 0x00001000;
}
