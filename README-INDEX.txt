================================================================================
README-INDEX: CodeBrix.Platform
Map of the README files in this repository
================================================================================

If you are an AI coding agent: find the NuGet package you are consuming below and read
its AGENT-README file in full. Read MAINTAINER-README.txt only if you are changing this
repository itself.

AGENT-README FILES (consumer documentation, one per NuGet package)
------------------------------------------------------------------
  AGENT-README.txt
      CodeBrix.Platform.ApacheLicenseForever,
      CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever,
      CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever,
      CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever,
      CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever,
      CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever,
      CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever,
      CodeBrix.Platform.Runtime.Skia.FrameBuffer.Emulated.ApacheLicenseForever,
      CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever
          The cross-platform WinUI-XAML framework rendered with Skia, its base Skia
          runtime, and the six shipping platform heads (Win32, WPF, X11, Wayland,
          Linux frame buffer and macOS) plus the IDE-only emulated frame buffer
          head. START HERE for any CodeBrix.Platform application.
          Includes independent UseSystemAppName("My App") and UseSystemMenuBar()
          options in UseMacOS(...), the first-visible
          MenuBar selection rule, and native menu behavior. Native build details:
          src/Platform.UI.Runtime.Skia.MacOS/PlatformNativeMac/README.md.
          MAINTAINER-README.txt lists the AppKit regression probe and local
          macOS preview-package build command.

  src/Platform.UI.Runtime.Skia.PlayTest/AGENT-README.txt
    Package: CodeBrix.Platform.PlayTest.ApacheLicenseForever
    The test head: run the real application offscreen on a virtual Skia screen
    and drive it with locators, actions and retrying assertions. Covers
    test-project setup, fixtures, orientation/theme/preview/action-delay
    preferences, command-line switches, automatic screenshot recording,
    scripted file/folder pickers, the recording launcher, held keys, window
    close/minimize requests, option selection, element screenshots,
    checked controls, editors, menus, drags and limitations. Maintainer notes: MAINTAINER-README.txt, THE PLAYTEST HEAD
    PACKAGE.

    src/Platform.UI.Runtime.Skia.PlayTest.OpenGL/AGENT-README.txt
      CodeBrix.Platform.PlayTest.OpenGL.ApacheLicenseForever
          Real OpenGL contexts for PlayTests of applications that use the
          Graphics3DGL add-in (surfaceless EGL on Linux, WGL on Windows, ANGLE
          on macOS); registered with CodeBrixPlayTestOpenGL.Register(). The
          PlayTest guide's OPENGL section covers opting out, the failure
          messages and asserting GL content with PixelStats.

    Related PlayTest guide:
      samples/CodeBrixPlatform/PlayTestDemo/README.md
        Runnable examples: demo and PlayTests project structure, build/run
        commands, coverage and validation. Start here to run the dedicated
        control/picker demo or add general PlayTest regression examples.
        Includes Windows and macOS native preview pixel-check commands.

  src/AddIns/Platform.WinUI.Graphics2DSK/AGENT-README.txt
      CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever
          Immediate-mode 2D drawing: one XAML element you draw into with SkiaSharp.

  src/AddIns/Platform.WinUI.Graphics3DGL/AGENT-README.txt
      CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever
          OpenGL for a XAML page: two GPU-rendered elements plus two helpers for
          off-screen GPU work.

  src/AddIns/Platform.UI.Lottie/AGENT-README.txt
      CodeBrix.Platform.Lottie.ApacheLicenseForever
          Plays Lottie (Bodymovin JSON) vector animations in a XAML page.

  src/AddIns/Platform.UI.Svg/AGENT-README.txt
      CodeBrix.Platform.Svg.ApacheLicenseForever
          Makes the core framework's SvgImageSource actually render SVG content.

  src/AddIns/CodeBrix.Platform.SkiaSharp.Views/AGENT-README.txt
      CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever
          The SkiaSharp XAML view types (SKXamlCanvas and friends) in SkiaSharp's own
          namespace, for CodeBrix.Platform applications.

  src/AddIns/Platform.UI.MediaPlayer.Skia/AGENT-README.txt
      CodeBrix.Platform.MediaPlayer.LgplLicenseForever,
      CodeBrix.Platform.WinUI.MediaPlayer.Skia.Win32.LgplLicenseForever,
      CodeBrix.Platform.WinUI.MediaPlayer.Skia.X11.LgplLicenseForever
          Makes the XAML MediaPlayerElement (audio and video playback) work on the
          Skia heads, via LibVLC. The same file is packed as the AGENT-README of the
          two superseded native-child-window projects that carry the other two ids
          above; those projects are packable and self-pack on a Release build, but
          they are excluded from the central pack driver and are never published -
          use the CodeBrix.Platform.MediaPlayer.LgplLicenseForever package instead.

  src/AddIns/Platform.UI.AdvancedTextEdit/AGENT-README.txt
      CodeBrix.Platform.AdvancedTextEdit.ApacheLicenseForever
          A full code/text editor control with the editing model of a professional
          code editor, on every head.

  src/AddIns/Platform.AppSettings/AGENT-README.txt
      CodeBrix.Platform.AppSettings.ApacheLicenseForever
          A persistent application-settings system for CodeBrix.Platform applications.

  src/AddIns/Platform.UI.AudioPlayer.Skia/AGENT-README.txt
      CodeBrix.Platform.AudioPlayer.ApacheLicenseForever
          Audio playback (WAV, MP3, Ogg Vorbis, FLAC, Opus), sound effects and MIDI
          synthesis through SoundFont, SFZ or Decent Sampler instruments.

  src/AddIns/Platform.UI.CommandBar/AGENT-README.txt
      CodeBrix.Platform.CommandBar.ApacheLicenseForever
          Desktop tool bars: trays, bars, groups, separators, spacers and command-bound
          buttons with SVG and raster icons.

  src/AddIns/Platform.UI.FlexPanel/AGENT-README.txt
      CodeBrix.Platform.FlexPanel.ApacheLicenseForever
          A CSS flexbox-style XAML layout panel.

  src/AddIns/Platform.UI.PlotterView/AGENT-README.txt
      CodeBrix.Platform.PlotterView.ApacheLicenseForever
          A chart view: PlotterControl, the XAML host for CodeBrix.Plotter plot models.

  src/AddIns/Platform.UI.TerminalView/AGENT-README.txt
      CodeBrix.Platform.TerminalView.ApacheLicenseForever
          A terminal emulator view: TerminalControl, the XAML renderer for a
          CodeBrix.Terminal engine.

  src/AddIns/Platform.UI.TextLayout/AGENT-README.txt
      CodeBrix.Platform.TextLayout.ApacheLicenseForever
          Pango-class text shaping and layout (HarfBuzz, bidi, font fallback) with no
          XAML and no application host required.

  src/AddIns/Platform.UI.VideoPlayer.Skia/AGENT-README.txt
      CodeBrix.Platform.VideoPlayer.ApacheLicenseForever
          Video playback (AV1 in WebM, Matroska and CodeBrix .cbv files) with a
          colour-grading effect chain, on the GPU where a head can give one.

  src/AddIns/Platform.UI.WebView.Skia/AGENT-README.txt
      CodeBrix.Platform.WebView.ApacheLicenseForever
          Makes the XAML WebView2 control work on the Skia desktop heads, downloads
          included.

  src-platforms/Platform.WinUI/AGENT-README.txt
      CodeBrix.Platform.WinUI.ApacheLicenseForever,
      CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever,
      CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever
          The CodeBrix "Simple" MVVM toolkit for Microsoft's own WinUI (Windows App
          SDK), plus its SkiaSharp and Lottie companions.

  src-platforms/Platform.WPF/AGENT-README.txt
      CodeBrix.Platform.WPF.ApacheLicenseForever
          The CodeBrix "Simple" MVVM toolkit for Microsoft's own WPF.

  src-platforms/Platform.Mobile/AGENT-README.txt
      CodeBrix.Platform.Mobile.ApacheLicenseForever
          The CodeBrix "Simple" MVVM toolkit for .NET MAUI.

MAINTAINER AND EXTRAS
---------------------
  MAINTAINER-README.txt
      Building, testing, packaging, versioning and provenance notes for maintainers.
  EXTRAS-README.txt
      Samples, tools and other non-package content in this repository.
  tools/MacOsWebViewHelper/README.md
      Native PlayTest WKWebView helper source and standalone universal build
      instructions for Intel and Apple Silicon Macs.

GENERAL
-------
  README.md
      Human-facing overview shown on GitHub and nuget.org.
  CODEBRIX-PLATFORM-README.md
      The family-wide catalogue of the CodeBrix.Platform packages.
  NOT-IMPLEMENTED.md
      What a "not implemented" exception from the framework means, and what to do
      about it.
  src/Platform.UI.Runtime.Skia.PlayTest.OpenGL/README.md
      Human-facing overview for the PlayTest OpenGL provider package (its
      package README).
  src-platforms/Platform.WinUI/README.md
      Human-facing overview for the WinUI toolkit packages.
  src-platforms/Platform.WPF/README.md
      Human-facing overview for the WPF toolkit package.
  src-platforms/Platform.Mobile/README.md
      Human-facing overview for the .NET MAUI toolkit package.
  THIRD-PARTY-NOTICES.txt
      What came from where, and under which licences. Packed into every package
      produced from this repository's root.
  src-platforms/Platform.WinUI/THIRD-PARTY-NOTICES.txt
  src-platforms/Platform.WPF/THIRD-PARTY-NOTICES.txt
  src-platforms/Platform.Mobile/THIRD-PARTY-NOTICES.txt
      The same record for each native-framework toolkit family, packed into that
      family's packages.
  README-INDEX.txt
      This file.
