using System.Runtime.InteropServices;
using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Fohjin.DDD.BankApplication.UITests;

// FlaUI's Application.GetAllTopLevelWindows(automation) reliably misses at least one real,
// visible, correctly-titled top-level window belonging to a target process (confirmed against
// both the WinForms and WPF clients: raw Win32 EnumWindows sees windows FlaUI's own enumeration
// does not). Finding the HWND via plain Win32 first and wrapping only that specific handle
// through FlaUI (AutomationBase.FromHandle) sidesteps whatever GetAllTopLevelWindows' scope
// query is failing to enumerate - shared by the WinForms and WPF workflow tests.
internal static class Win32WindowFinder
{
    public static Window? WaitForWindow(UIA3Automation automation, int processId, string titlePart, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var hwnd = FindHwnd(processId, titlePart);
            if (hwnd != IntPtr.Zero)
                return automation.FromHandle(hwnd).AsWindow();

            Thread.Sleep(500);
        }

        return null;
    }

    public static IntPtr FindHwnd(int processId, string titlePart)
    {
        var found = IntPtr.Zero;
        NativeMethods.EnumWindows((candidate, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(candidate, out var windowProcessId);
            if (windowProcessId != processId || !NativeMethods.IsWindowVisible(candidate))
                return true;

            var length = NativeMethods.GetWindowTextLength(candidate);
            if (length == 0)
                return true;

            var builder = new StringBuilder(length + 1);
            NativeMethods.GetWindowText(candidate, builder, builder.Capacity);
            if (!builder.ToString().Contains(titlePart, StringComparison.OrdinalIgnoreCase))
                return true;

            found = candidate;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    public static void BringToForeground(IntPtr hwnd) => NativeMethods.SetForegroundWindow(hwnd);

    // Dev databases persist across every run of this suite, so a list can easily have more
    // entries than fit the visible viewport by now - clicking an off-screen item throws
    // NoClickablePointException. ScrollItemPattern is the correct UIA way to bring it into view
    // without relying on a screen coordinate that doesn't exist yet.
    public static void ScrollIntoView(AutomationElement element)
    {
        if (element.Patterns.ScrollItem.IsSupported)
            element.Patterns.ScrollItem.Pattern.ScrollIntoView();
    }

    private static class NativeMethods
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
