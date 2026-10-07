using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI.Core;

namespace PlayTestDemo;

/// <summary>Counts the main window's close, visibility and activation events, and holds the
/// "unsaved changes" flag that makes the window refuse to close, like a "save changes?" prompt.</summary>
public static class WindowLifecycle
{
    public static bool KeepOpen { get; set; }
    public static int CloseRequests { get; private set; }
    public static int CancelledCloses { get; private set; }
    public static int Closed { get; private set; }
    public static int Hidden { get; private set; }
    public static int Shown { get; private set; }
    public static int Deactivated { get; private set; }
    public static event EventHandler Changed;

    public static string Summary =>
        $"Close requests {CloseRequests} · cancelled {CancelledCloses} · closed {Closed} · hidden {Hidden} · shown {Shown} · deactivated {Deactivated}";

    public static void Attach(Window window)
    {
        window.AppWindow.Closing += OnClosing;
        window.Closed += (_, _) => { Closed++; Raise(); };
        window.VisibilityChanged += (_, args) =>
        {
            if (args.Visible) Shown++;
            else Hidden++;
            Raise();
        };
        window.Activated += (_, args) =>
        {
            if (args.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                Deactivated++;
                Raise();
            }
        };
    }

    private static void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        CloseRequests++;
        if (KeepOpen)
        {
            args.Cancel = true;
            CancelledCloses++;
        }
        Raise();
    }

    private static void Raise() => Changed?.Invoke(null, EventArgs.Empty);
}
