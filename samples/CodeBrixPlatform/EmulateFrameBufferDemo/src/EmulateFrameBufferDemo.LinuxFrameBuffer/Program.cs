using CodeBrix.Platform.UI.Hosting;
using System;
using Windows.Graphics.Display;

namespace EmulateFrameBufferDemo;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = CodeBrixPlatformHostBuilder.Create()
            .App(() => new App())
            
            //.UseLinuxFrameBuffer() //no application start orientation specified
            
            // The orientation this app WANTS TO BE, worked out against the panel's
            // native geometry. Without isPreferredOrientation the value would instead
            // be a rotation to apply, and Landscape would mean "no rotation".
            .UseLinuxFrameBuffer(fb => fb
                .Orientation(DisplayOrientations.Landscape, isPreferredOrientation: true) //wants Landscape: upright on a landscape panel, sideways on a portrait one
                .AutoRotationEnabled(true))
                //.AutoRotationEnabled(DisplayOrientations.Landscape, DisplayOrientations.LandscapeFlipped))
            //.UseLinuxFrameBuffer(fb => fb.Orientation(DisplayOrientations.LandscapeFlipped, isPreferredOrientation: true)) //wants Landscape (upside-down)
            
            //.UseLinuxFrameBuffer(fb => fb.Orientation(DisplayOrientations.Portrait, isPreferredOrientation: true)) //wants Portrait: upright on a portrait panel, sideways on a landscape one
            //.UseLinuxFrameBuffer(fb => fb.Orientation(DisplayOrientations.PortraitFlipped, isPreferredOrientation: true)) //wants Portrait: (upside-down)
           
            .UseDirectSkiaCanvasMode() //Experimental - should be safe to leave enabled
            .Build();

        host.Run();
    }
}
