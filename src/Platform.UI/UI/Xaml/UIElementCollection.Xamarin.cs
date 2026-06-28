using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.Extensions;
using System;
using CodeBrix.Platform.UI.Controls;
#if false
using _View = Android.Views.View;
using _BindableView = CodeBrix.Platform.UI.Controls.BindableView;
#elif false
using _View = UIKit.UIView;
using _BindableView = CodeBrix.Platform.UI.Controls.BindableUIView;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class UIElementCollection : IList<UIElement>, IEnumerable<UIElement>
	{

		// This method is present to enable allocation-less enumeration.
		public Enumerator GetEnumerator() => new Enumerator(_owner);

		IEnumerator<UIElement> IEnumerable<UIElement>.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<UIElement>, IEnumerator
		{
			private List<_View>.Enumerator _inner;

			internal Enumerator(_BindableView owner)
			{
				_inner = owner.GetChildrenEnumerator();
			}

			public UIElement Current => _inner.Current as UIElement;

			object IEnumerator.Current => Current;

			public void Dispose() => _inner.Dispose();

			public bool MoveNext() => _inner.MoveNext();

			public void Reset() => ((IEnumerator)_inner).Reset();
		}
	}
}
