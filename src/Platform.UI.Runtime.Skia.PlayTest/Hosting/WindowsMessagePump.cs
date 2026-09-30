using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.PlayTest.Hosting;

// Optional Windows components such as WebView2 dispatch COM callbacks through messages
// on their owning STA. Keep processing the managed queue while pumping that thread.
internal static class WindowsMessagePump
{
    internal static void Run(BlockingCollection<Action> queue)
    {
        while (!queue.IsCompleted)
        {
            if (queue.TryTake(out var action, 5)) action();
            for (var count = 0; count < 64 && PeekMessage(out var message, IntPtr.Zero, 0, 0, 1); count++)
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public IntPtr Window;
        public uint Id;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X;
        public int Y;
        public uint Private;
    }

    [DllImport("user32.dll", EntryPoint = "PeekMessageW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out Message message, IntPtr window, uint min, uint max, uint remove);
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll", EntryPoint = "DispatchMessageW", ExactSpelling = true)]
    private static extern IntPtr DispatchMessage(ref Message message);
}
