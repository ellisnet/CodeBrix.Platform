#if DEBUG
#nullable enable

using CodeBrix.Platform.Extensions.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.Extensions.Disposables;
using System.Text;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Diagnostics.Eventing;
using CodeBrix.Platform.UI.DataBinding;

namespace Microsoft.UI.Xaml
{
	partial class DependencyObjectExtensions
	{
		/// <summary>
		/// Debugging method to get the resource key associated with this resource, if it came from a <see cref="ResourceDictionary"/>.
		/// </summary>
		/// <remarks>Note: The DEBUG_SET_RESOURCE_SOURCE symbol must be set in <see cref="ResourceDictionary"/> for this to return a value.</remarks>
		internal static string GetResourceNameDebug(this DependencyObject obj)
		{
			var source = ResourceDictionary.GetResourceSource(obj);

			return source?.ResourceKey.Key ?? "No associated key found. Make sure you uncommented '//#define DEBUG_SET_RESOURCE_SOURCE' in ResourceDictionary.cs";

		}

		internal static ResourceDictionary? GetContainingResourceDictionaryDebug(this DependencyObject obj)
		{
			var source = ResourceDictionary.GetResourceSource(obj);

			return source?.ContainingDictionary;
		}
	}
}

#endif
