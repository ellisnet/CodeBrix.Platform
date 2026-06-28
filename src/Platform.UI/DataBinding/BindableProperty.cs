#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.DataBinding //Was previously: Uno.UI.DataBinding
{
	/// <summary>
	/// Defines a bindable property implementation.
	/// </summary>
	public class BindableProperty : IBindableProperty
	{
		public BindableProperty(DependencyProperty property)
		{
			DependencyProperty = property;
			PropertyType = property.Type;
		}


		/// <summary>
		/// This ctor is available for backward compatibility. The BindableTypeProvidersSourceGenerator uses the single-parameter ctor
		/// </summary>
		public BindableProperty(
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
			Type propertyType,
			PropertyGetterHandler getter,
			PropertySetterHandler? setter)
		{
			Getter = getter;
			Setter = setter;
			PropertyType = propertyType;
		}

		public PropertyGetterHandler? Getter { get; }

		public PropertySetterHandler? Setter { get; }

		[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
		public Type PropertyType { get; }

		public DependencyProperty? DependencyProperty { get; }
	}
}
