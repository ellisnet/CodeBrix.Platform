using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.Extensions;
using System;
using View = Microsoft.UI.Xaml.UIElement;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class UIElementCollection
	{
		private readonly List<UIElement> _elements;
		private readonly FrameworkElement _owner;

		public UIElementCollection(FrameworkElement view)
		{
			_elements = view._children;
			_owner = view;
		}

		private void AddCore(View item) => _owner.AddChild(item);

		private IEnumerable<View> ClearCore()
		{
			var old = _elements.ToArray();
			_elements.Clear();

			return old;
		}

		private bool ContainsCore(View item) => _elements.Contains(item);

		//WPE1-21: ItemsControl's Reset path copies the panel's children (ToArray -> CopyTo) before it cleans them up
		//  (WPE1-17), so the unit-test collection must support the copy like the platform one does
		private void CopyToCore(View[] array, int arrayIndex) => _elements.CopyTo(array, arrayIndex);

		private int CountCore() => _elements.Count;

		private View GetAtIndexCore(int index) => _elements[index];

		public IEnumerator<View> GetEnumerator() => _elements.GetEnumerator();

		private int IndexOfCore(View item) => _elements.IndexOf(item);

		private void InsertCore(int index, View item) => _owner.AddChild(item, index);

		private void MoveCore(uint oldIndex, uint newIndex)
		{
			throw new NotImplementedException();
		}

		private View RemoveAtCore(int index)
		{
			var item = _elements.ElementAtOrDefault(index);
			if (item != null)
			{
				_owner.RemoveChild(item);
			}
			return item;
		}

		private bool RemoveCore(View item) => _owner.RemoveChild(item) != null;

		private View SetAtIndexCore(int index, View value) => _elements[index] = value;
	}
}
