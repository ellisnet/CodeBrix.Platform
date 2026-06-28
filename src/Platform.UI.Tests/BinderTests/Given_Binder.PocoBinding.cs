using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Tests.BinderTests.DependencyPropertyPath //Was previously: Uno.UI.Tests.BinderTests.DependencyPropertyPath
{
	[TestClass]
	public partial class Given_Binder_PocoBinding
	{
		[TestMethod]
		public void When_PropertyChanged_And_SetBinding()
		{
			var SUT = new MyObject();

			SUT.MyProperty = "41";

			SUT.SetBinding("MyProperty", new Binding { Path = "Value", Mode = BindingMode.TwoWay });

			SUT.DataContext = new { Value = "42" };

			Assert.AreEqual("42", SUT.MyProperty);
		}

		[TestMethod]
		public void When_SetBinding_And_PropertyChanged()
		{
			var SUT = new MyObject();

			SUT.SetBinding("MyProperty", new Binding { Path = "Value", Mode = BindingMode.TwoWay });

			SUT.MyProperty = "41";

			SUT.DataContext = new { Value = "42" };

			Assert.AreEqual("42", SUT.MyProperty);
		}

		public partial class MyObject : DependencyObject
		{
			public MyObject()
			{
			}

			private string _myProperty;

			public string MyProperty
			{
				get { return _myProperty; }
				set
				{
					_myProperty = value;

					SetBindingValue(value);
				}
			}
		}
	}
}
