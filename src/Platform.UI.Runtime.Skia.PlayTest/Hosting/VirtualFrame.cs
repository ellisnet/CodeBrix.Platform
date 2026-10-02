namespace CodeBrix.Platform.PlayTest.Hosting;

// A screen instance identifies one orientation transition, including Landscape -> Portrait -> Landscape.
internal sealed class VirtualScreen
{
    internal ScreenOrientation Orientation { get; }
    internal int Width => Orientation == ScreenOrientation.Portrait ? 1080 : 1920;
    internal int Height => Orientation == ScreenOrientation.Portrait ? 1920 : 1080;
    internal VirtualScreen(ScreenOrientation orientation) => Orientation = orientation;
}

// Keep dimensions attached to their pixels across rendering, screenshots and the preview pipe.
internal sealed class VirtualFrame
{
    internal VirtualScreen Screen { get; }
    internal byte[] Pixels { get; }
    internal int Width => Screen.Width;
    internal int Height => Screen.Height;
    internal VirtualFrame(VirtualScreen screen, byte[] pixels) { Screen = screen; Pixels = pixels; }
}
