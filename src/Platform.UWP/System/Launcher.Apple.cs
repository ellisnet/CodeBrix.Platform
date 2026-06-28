using System;
using System.Threading.Tasks;
#if false
using UIKit;
#else
using AppKit;
using Foundation;
#endif
using AppleUrl = global::Foundation.NSUrl;

namespace Windows.System
{
	public static partial class Launcher
	{
		public static
#if false
			async
#endif
			Task<bool> LaunchUriPlatformAsync(Uri uri)
		{
			if (IsSpecialUri(uri) && CanHandleSpecialUri(uri))
			{
#if false
				return await HandleSpecialUri(uri);
#else
				return Task.FromResult(HandleSpecialUri(uri));
#endif
			}

			var appleUrl = new AppleUrl(uri.OriginalString);
#if false
			return await UIApplication.SharedApplication.OpenUrlAsync(appleUrl, new UIApplicationOpenUrlOptions());
#else
			return Task.FromResult(NSWorkspace.SharedWorkspace.OpenUrl(
				appleUrl));
#endif
		}


		public static Task<LaunchQuerySupportStatus> QueryUriSupportPlatformAsync(
			Uri uri,
			LaunchQuerySupportType launchQuerySupportType)
		{
			bool canOpenUri;
			if (!IsSpecialUri(uri))
			{
#if false
				canOpenUri = UIApplication.SharedApplication.CanOpenUrl(
					new AppleUrl(uri.OriginalString));
#else
				canOpenUri = NSWorkspace.SharedWorkspace.UrlForApplication(new NSUrl(uri.AbsoluteUri)) != null;
#endif
			}
			else
			{
				canOpenUri = CanHandleSpecialUri(uri);
			}

			var supportStatus = canOpenUri ?
				LaunchQuerySupportStatus.Available : LaunchQuerySupportStatus.NotSupported;

			return Task.FromResult(supportStatus);
		}
	}
}
