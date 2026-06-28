using System;
using CodeBrix.Platform.UI.Xaml;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI //Was previously: Uno.UI
{
	/// <summary>
	/// Metadata update handler used to reset caches when changes are applied
	/// by the hot reload engine.
	/// </summary>
	internal class RuntimeTypeMetadataUpdateHandler
	{
		public static void ClearCache(Type[] types)
		{
			DataBinding.BindingPropertyHelper.ClearCaches();
		}
	}
}
