using CodeBrix.Platform;
using CodeBrix.Platform.Extensions;
using Windows.Foundation;

namespace Windows.ApplicationModel.Activation;

/// <summary>
/// Provides a dismissal event and image location information for the app's splash screen.
/// </summary>
/// <remarks>This API is currently not connected to platform-specific splash screens.</remarks>
public sealed partial class SplashScreen
{
	internal SplashScreen()
	{
	}

#pragma warning disable CS0067 // The event 'SplashScreen.Dismissed' is never used
	/// <summary>
	/// Fires when the app's splash screen is dismissed.
	/// </summary>
	[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public event TypedEventHandler<SplashScreen, object> Dismissed;
#pragma warning restore CS0067 // The event 'SplashScreen.Dismissed' is never used

	/// <summary>
	/// The coordinates of the app's splash screen image relative to the window.
	/// </summary>
	[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public Rect ImageLocation { get; }
}
