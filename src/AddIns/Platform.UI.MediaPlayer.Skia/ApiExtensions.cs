using Microsoft.UI.Xaml.Controls;
using Windows.Media.Playback;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Media.Playback;
using CodeBrix.Platform.UI.MediaPlayer.Skia;

// These registrations are discovered at build time by the XAML source generator, which emits
// OS-gated ApiExtensibility.Register calls into the consuming app. Reference this AddIn once,
// from the app's .Core project: the Windows (Win32 and Skia-on-WPF) and Linux (X11, Wayland,
// FrameBuffer) heads activate it; on the macOS head neither condition matches, so the AddIn is
// inert and the head's built-in AVFoundation media support registers instead.

[assembly: ApiExtension(
	typeof(IMediaPlayerExtension),
	typeof(SkiaMediaPlayerExtension),
	ownerType: typeof(MediaPlayer),
	operatingSystemCondition: "linux")]

[assembly: ApiExtension(
	typeof(IMediaPlayerExtension),
	typeof(SkiaMediaPlayerExtension),
	ownerType: typeof(MediaPlayer),
	operatingSystemCondition: "windows")]

[assembly: ApiExtension(
	typeof(IMediaPlayerPresenterExtension),
	typeof(SkiaMediaPlayerPresenterExtension),
	ownerType: typeof(MediaPlayerPresenter),
	operatingSystemCondition: "linux")]

[assembly: ApiExtension(
	typeof(IMediaPlayerPresenterExtension),
	typeof(SkiaMediaPlayerPresenterExtension),
	ownerType: typeof(MediaPlayerPresenter),
	operatingSystemCondition: "windows")]
