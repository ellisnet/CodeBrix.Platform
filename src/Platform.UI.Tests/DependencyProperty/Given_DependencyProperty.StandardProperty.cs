using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Tests.BinderTests_StandardProperty //Was previously: Uno.UI.Tests.BinderTests_StandardProperty
{
	[TestClass]
	public partial class Given_DependencyProperty_StandardProperty
	{
		[TestMethod]
		public void When_SimpleInheritance()
		{
			var SUT = new MyObject();

			SUT.SetBinding("Name", new Binding());

			SUT.DataContext = "Test";

			Assert.AreEqual("Test", SUT.Name);
		}
	}

	public partial class MyObject : DependencyObject
	{
		public string Name
		{
			get => _name;
			set => _name = value;
		}

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
		private string _name;

		private void OnInnerObjectChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

	}
}
