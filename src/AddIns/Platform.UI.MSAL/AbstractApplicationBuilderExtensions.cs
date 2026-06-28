using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;

namespace CodeBrix.Platform.UI.MSAL //Was previously: Uno.UI.MSAL
{
	public static class AbstractApplicationBuilderExtensions
	{
		/// <summary>
		/// Add required helpers for the current CodeBrix platform.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T WithCodeBrixHelpers<T>(this T builder)
			where T : AbstractApplicationBuilder<T>
		{
#if false
			(builder as PublicClientApplicationBuilder)?.WithParentActivityOrWindow(() => ContextHelper.Current as Android.App.Activity);
#elif false
#pragma warning disable CA1422 // Validate platform compatibility
			(builder as PublicClientApplicationBuilder)?.WithParentActivityOrWindow(() => UIKit.UIApplication.SharedApplication?.KeyWindow?.RootViewController);
#pragma warning restore CA1422 // Validate platform compatibility
#elif false
			builder.WithHttpClientFactory(WasmHttpFactory.Instance);
#endif
			return builder;
		}
	}
}
