using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Tests.BinderTests_TemplatedParent //Was previously: Uno.UI.Tests.BinderTests_TemplatedParent
{
	public partial class MyObject : FrameworkElement
	{
		public MyObject InnerWithValueInheritsDataContext
		{
			get { return (MyObject)GetValue(InnerWithDataContextValueProperty); }
			set { SetValue(InnerWithDataContextValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for InnerWithDataContextValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty InnerWithDataContextValueProperty =
			DependencyProperty.Register(
				name: "InnerWithValueInheritsDataContext",
				propertyType: typeof(MyObject),
				ownerType: typeof(MyObject),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext
				)
			);

		#region InnerObject DependencyProperty

		public MyObject InnerObject
		{
			get { return (MyObject)GetValue(InnerObjectProperty); }
			set { SetValue(InnerObjectProperty, value); }
		}

		// Using a DependencyProperty as the backing store for InnerObject.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty InnerObjectProperty =
			DependencyProperty.Register(
				name: "InnerObject",
				propertyType: typeof(MyObject),
				ownerType: typeof(MyObject),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.LogicalChild,
					propertyChangedCallback: (s, e) => ((MyObject)s)?.OnInnerObjectChanged(e)
				)
			);


		private void OnInnerObjectChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

	}
}
