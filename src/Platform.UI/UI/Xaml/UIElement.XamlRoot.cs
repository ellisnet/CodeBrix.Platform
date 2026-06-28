#nullable enable

using System;
using CodeBrix.Platform.UI.DataBinding;
using CodeBrix.Platform.UI.Extensions;
using CodeBrix.Platform.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

public partial class UIElement : DependencyObject, IXUidProvider
{
	private ManagedWeakReference? _visualTreeCacheWeakReference;

	public XamlRoot? XamlRoot
	{
		get => XamlRoot.GetForElement(this);
		set => XamlRoot.SetForElement(this, XamlRoot, value);
	}

	internal VisualTree? VisualTreeCache
	{
		get => _visualTreeCacheWeakReference?.IsDisposed == false ?
			_visualTreeCacheWeakReference.Target as VisualTree : null;
		set => _visualTreeCacheWeakReference = WeakReferencePool.RentWeakReference(this, value);
	}
}
