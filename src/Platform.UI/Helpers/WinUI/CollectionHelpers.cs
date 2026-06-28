using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Helpers.WinUI //Was previously: Uno.UI.Helpers.WinUI
{
	internal static class CollectionHelper
	{
		internal static void UniquePushBack<T>(IList<T> items, T newItem)
		{
			if (!items.Contains(newItem))
			{
				items.Add(newItem);
			}
		}
	}
}
