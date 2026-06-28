using System;
using System.Collections.Generic;
using System.Text;

namespace CodeBrix.Platform.UI.DataBinding //Was previously: Uno.UI.DataBinding
{
	public interface IBindingItem
	{
		string PropertyName { get; }
		Type PropertyType { get; }
		object DataContext { get; }
	}
}
