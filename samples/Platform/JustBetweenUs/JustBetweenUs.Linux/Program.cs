using CodeBrix.Platform.UI.Hosting;
using System;

// ReSharper disable CheckNamespace

//TODO: Notes about running this app on Linux on an ARM64 machine, as of 3/15/2026:
/*
The Debian FreeType package actually does include BDF support and fully exports the 
  FT_Get_BDF_Property symbol.
The true culprit is a known compilation bug in the Linux ARM64 build of the SkiaSharp 
  native asset.
  
The Missing Dependency Link
When software is compiled for Linux, shared libraries (like libSkiaSharp.so) are 
  supposed to include an internal list of other libraries they depend on, known as 
  DT_NEEDED entries.
The maintainers who compile the official libSkiaSharp.so binary for ARM64 accidentally 
  failed to link libfreetype.so.6 (and often libuuid.so.1) as required dynamic 
  dependencies during their build process.
Because libSkiaSharp.so doesn't explicitly declare that it needs FreeType, the Linux 
  dynamic linker (ld.so) doesn't automatically load the FreeType library into memory 
  when SkiaSharp initializes. When SkiaSharp eventually attempts to execute 
  FT_Get_BDF_Property, the system throws an "undefined symbol" error because it hasn't 
  loaded the library containing that function into the application's memory space.

Why the Workaround Works
When you use the export LD_PRELOAD=/usr/lib/aarch64-linux-gnu/libfreetype.so.6 command, 
  you are forcing the operating system to load the FreeType library globally before 
  the .NET application even starts. By the time SkiaSharp asks for FT_Get_BDF_Property, 
  the symbol is already waiting in the global memory pool, allowing the app to run 
  smoothly.
  
So, I am having to run the app, for now, with:
> LD_PRELOAD=/usr/lib/aarch64-linux-gnu/libfreetype.so.6 dotnet run JustBetweenUs.Linux.csproj

More notes about the current bug:
The community and maintainers are fully aware of the missing DT_NEEDED entries for 
both FT_Get_BDF_Property (from FreeType) and uuid_generate_random (from libuuid) on 
Linux ARM64 builds.
   
Here are the primary issues where this is being discussed and resolved:
 - Issue #3272: [BUG] libSkiaSharp.so: undefined symbol: uuid_generate_random on 
     Debian Bookworm ARM64 — This is the central thread for the ARM64 dependency 
     discussion, where the missing FT_Get_BDF_Property linkage is heavily detailed 
     alongside the UUID bug.
 - Issue #3436: [BUG] undefined symbol exception thrown in linux ARM-64 — A recent 
     issue specifically highlighting the crash caused by these exact missing symbols 
     after upgrading SkiaSharp versions.
 - Issue #3229: [BUG] linux-riscv64/libSkiaSharp.so: undefined symbol: 
     FT_Get_BDF_Property — While initially opened for RISC-V architectures, 
     this issue overlaps significantly with the ARM64 missing symbol bug and tracks 
     the same root cause.
The repository maintainers have recently been actively merging pull requests to fix 
  the cross-compilation sysroot. This means future releases of the SkiaSharp NuGet 
  package should correctly link these native libraries out of the box.
*/

/*
Second Raspberry Pi problem:
I was able to get the app to run on Raspberry Pi OS (64-bit) based on Debian Trixie 13,
but the app was appearing with borderless window, so it could not be resized and there
was no close (X) button - to exit out of the app.
This was fixed by a configuration change in my Raspberry Pi configuration:
I edited the file at: ~/.config/labwc/rc.xml
And added the following <windowRules> section:
<openbox_config>
  <!-- ...existing config... -->

  <windowRules>
    <windowRule identifier="*" serverDecoration="yes" />
  </windowRules>
</openbox_config>

Then I had to run: killall -SIGHUP labwc
...in order to reload.
Then, in subsequent runs, my app window did have a border.

It might have been better to add (instead):
<windowRule identifier="JustBetweenUs*" serverDecoration="yes" />
...so I wasn't changing any global behavior, but I didn't try that.
*/

namespace JustBetweenUs;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = CodeBrixPlatformHostBuilder.Create()
            .App(() => new App())
            .UseLinuxX11()
            .Build();

        host.Run();
    }
}
