using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Albeoris.Games.FF8.Toolset.Infrastructure;

internal sealed class ConsoleWindowDialogHost
{
    private const Int32 RestoreWindowCommand = 9;

    public DialogResult Show(CommonDialog dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);
        IntPtr consoleWindowHandle = GetConsoleWindow();
        if (consoleWindowHandle == IntPtr.Zero)
            return dialog.ShowDialog();

        try
        {
            Activate(consoleWindowHandle);
            return dialog.ShowDialog(new WindowOwner(consoleWindowHandle));
        }
        finally
        {
            Activate(consoleWindowHandle);
        }
    }

    private static void Activate(IntPtr windowHandle)
    {
        if (IsIconic(windowHandle))
            _ = ShowWindow(windowHandle, RestoreWindowCommand);

        IntPtr foregroundWindow = GetForegroundWindow();
        UInt32 currentThreadId = GetCurrentThreadId();
        UInt32 foregroundThreadId = foregroundWindow == IntPtr.Zero
            ? 0
            : GetWindowThreadProcessId(foregroundWindow, out _);
        Boolean inputAttached = foregroundThreadId != 0 &&
            foregroundThreadId != currentThreadId &&
            AttachThreadInput(currentThreadId, foregroundThreadId, true);

        try
        {
            _ = BringWindowToTop(windowHandle);
            _ = SetForegroundWindow(windowHandle);
        }
        finally
        {
            if (inputAttached)
                _ = AttachThreadInput(currentThreadId, foregroundThreadId, false);
        }
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    private static extern UInt32 GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern UInt32 GetWindowThreadProcessId(IntPtr windowHandle, out UInt32 processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern Boolean AttachThreadInput(UInt32 idAttach, UInt32 idAttachTo, Boolean attach);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern Boolean BringWindowToTop(IntPtr windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern Boolean IsIconic(IntPtr windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern Boolean SetForegroundWindow(IntPtr windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern Boolean ShowWindow(IntPtr windowHandle, Int32 command);

    private sealed class WindowOwner(IntPtr handle) : IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }
}
