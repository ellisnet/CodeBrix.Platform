#nullable enable

using System;
using Windows.Foundation;
using CodeBrix.Platform.Helpers;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the <see cref="CoreWebView2.DownloadStarting"/> event.
/// </summary>
public partial class CoreWebView2DownloadStartingEventArgs : EventArgs
{
	internal CoreWebView2DownloadStartingEventArgs(CoreWebView2DownloadOperation downloadOperation)
	{
		DownloadOperation = downloadOperation;
		ResultFilePath = downloadOperation.ResultFilePath;
		DeferralManager = new DeferralManager<Deferral>(handler => new Deferral(handler));
	}

	internal DeferralManager<Deferral> DeferralManager { get; }

	/// <summary>
	/// Gets the download operation whose start raised this event.
	/// </summary>
	public CoreWebView2DownloadOperation DownloadOperation { get; }

	/// <summary>
	/// Set to true to cancel the download before it starts; the partially downloaded file
	/// is not kept.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Gets or sets the absolute path of the file (including the file name) the download will be
	/// written to. Defaults to a collision-free name in the user's Downloads folder; assign a
	/// different path before the event handler (or a deferral taken from it) completes to change
	/// the download location.
	/// </summary>
	public string ResultFilePath { get; set; }

	/// <summary>
	/// Gets or sets whether the default download handling is suppressed. The CodeBrix.Platform
	/// Skia heads show no built-in download UI of their own, so on these heads this affects
	/// only the underlying browser's download UI where one exists (the Windows WebView2 heads).
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Returns a deferral that allows <see cref="Cancel"/> and <see cref="ResultFilePath"/> to be
	/// decided asynchronously (for example after showing a file-save picker). The download does
	/// not start until the deferral is completed.
	/// </summary>
	public Deferral GetDeferral() => DeferralManager.GetDeferral();
}
