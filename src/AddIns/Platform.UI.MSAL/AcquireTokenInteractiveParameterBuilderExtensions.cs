using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace CodeBrix.Platform.UI.MSAL //Was previously: Uno.UI.MSAL
{
	public static class AcquireTokenInteractiveParameterBuilderExtensions
	{
		/// <summary>
		/// Add required helpers for the current CodeBrix platform.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AcquireTokenInteractiveParameterBuilder WithCodeBrixHelpers(this AcquireTokenInteractiveParameterBuilder builder)
		{
#if false
			builder.WithCustomWebUi(WasmWebUi.Instance);
#endif
			return builder;
		}
	}
}
