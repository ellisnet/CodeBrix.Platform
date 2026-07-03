using CodeBrix.Platform.UI.Hosting;
using System;

// ReSharper disable CheckNamespace

namespace FileFolderDialogDemo;

internal class Program
{
    // Must be a synchronous STA Main: the native file/folder pickers show the Win32 Common Item
    // Dialog (IFileOpenDialog.Show), which requires the UI thread to be an STA. With 'async Task
    // Main' the [STAThread] attribute is ignored and the thread runs as MTA, so the modal Show()
    // never runs its message loop - the app appears to hang and no dialog opens. host.Run() pumps
    // the Win32 message loop synchronously on this STA thread.
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = CodeBrixPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWindowsWin32()
            .Build();

        host.Run();
    }
}
