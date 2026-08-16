using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia;
using System;
using Windows.Graphics.Display;

// ReSharper disable CheckNamespace

namespace JustBetweenUs;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = CodeBrixPlatformHostBuilder.Create()
            .App(() => new App())
            .UseLinuxFrameBuffer(fb => fb
                .Orientation(DisplayOrientations.Landscape, isPreferredOrientation: true)
                .AutoRotationEnabled(true)
                .UseOrientationSensor()  //inert under CodeBrix.Develop launches (they pin
                                         //  CODEBRIX_FRAMEBUFFER_ORIENTATION_SOURCE=develop);
                                         //  live when launched standalone on the device
                .EnableSoftwareKeyboard(new SoftwareKeyboardOptions{
                    ShowDismissKey = true,  //default behavior = true
                    //ShowDismissKey = false,
                    AllowLockOn = true,
                    //AllowLockOn = false,  //default behavior = false
                    //KeyHeight = SoftwareKeyHeight.PortraitFullLandscapeFull,  //default behavior = PortraitFullLandscapeFull
                    KeyHeight = SoftwareKeyHeight.PortraitFullLandscapeHalf,
                })
                .EnableSimpleTextClipboard()
            )
            .UseDirectSkiaCanvasMode()
            .Build();

        host.Run();
    }
}
