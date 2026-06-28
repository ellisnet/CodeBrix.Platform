#if true
#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;

namespace CodeBrix.Platform.AuthenticationBroker //Was previously: Uno.AuthenticationBroker
{
	public partial class WebAuthenticationBrokerProvider : IWebAuthenticationBrokerProvider
	{
		internal static TimeSpan Timeout => WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout;

		public Uri GetCurrentApplicationCallbackUri()
		{
			var defaultUri = WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri;

			if (defaultUri != null)
			{
				// A default one is defined: we're using it instead of using
				// a discovery mechanism.

				//The Android Platform does not handle the last slash in Uri automatically
#if false
				return defaultUri.TrimEndUriSlash();
#else
				return defaultUri;
#endif

			}

			var schemes = this.GetApplicationCustomSchemes().ToArray();

			if (schemes.Length == 0)
			{
				throw new InvalidOperationException("No custom scheme found for this application.");
			}

			if (schemes.Length > 1)
			{
				var message =
					"More than one custom scheme is defined for this application.\n" +
					$"CodeBrix will use the first one found ({schemes[0]}), but you may want" +
					"to specify which one to use by setting " +
					"WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri.\n" +
					"Found schemes: " + schemes.JoinBy(" ");
				this.Log().Warn(message);
			}

			var uri = new Uri(schemes[0] + WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultCallbackPath);
#if false
			uri = uri.TrimEndUriSlash();
#endif
			return uri;
		}

		public async Task<WebAuthenticationResult> AuthenticateAsync(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			return await this.AuthenticateAsyncCore(options, requestUri, callbackUri, ct);
		}

	}
}
#endif
