#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;

namespace CodeBrix.Platform.AuthenticationBroker //Was previously: Uno.AuthenticationBroker
{
	partial class WebAuthenticationBrokerProvider
	{
		protected virtual IEnumerable<string> GetApplicationCustomSchemes()
		{
			throw new NotImplementedException();
		}

		protected virtual Task<WebAuthenticationResult> AuthenticateAsyncCore(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			throw new NotImplementedException();
		}
	}
}
