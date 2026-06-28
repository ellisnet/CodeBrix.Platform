using System;
using Foundation;
using Windows.Storage.Internal;

namespace CodeBrix.Platform.Storage.Internal; //Was previously: Uno.Storage.Internal

internal static class NSUrlExtensions
{
	public static IDisposable BeginSecurityScopedAccess(this NSUrl nsUrl) =>
		SecurityScopeManager.BeginScope(nsUrl);
}
