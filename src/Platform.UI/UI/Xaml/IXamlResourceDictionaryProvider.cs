using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using CodeBrix.Platform.Diagnostics.Eventing;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.UI.DataBinding;
using CodeBrix.Platform.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Resources;

namespace CodeBrix.Platform.UI //Was previously: Uno.UI
{
	/// <summary>
	/// Provides lazy initialization for a resource dictionary.
	/// </summary>
	/// <remarks> Normally only implemented and referenced by Xaml-generated code.</remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IXamlResourceDictionaryProvider
	{
		ResourceDictionary GetResourceDictionary();
	}
}
