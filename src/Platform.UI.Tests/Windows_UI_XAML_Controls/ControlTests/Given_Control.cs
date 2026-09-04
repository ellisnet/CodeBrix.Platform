using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Tests.ControlTests //Was previously: Uno.UI.Tests.ControlTests
{
	[TestClass]
	public partial class Given_Control
	{
		[TestMethod]
		public void When_ManuallyApplyTemplate()
		{
			var current = FeatureConfiguration.Control.UseLegacyLazyApplyTemplate;
			try
			{
				FeatureConfiguration.Control.UseLegacyLazyApplyTemplate = true;
				var templatedRoot = default(UIElement);
				var sut = new MyControl
				{
					Template = new ControlTemplate(() => templatedRoot = new Grid())
				};

				Assert.IsNull(sut.TemplatedRoot);
				Assert.IsNull(templatedRoot);

				new Grid().Children.Add(sut); // This kind-of simulate that the control is in the visual tree.

				Assert.IsNull(sut.TemplatedRoot);
				Assert.IsNull(templatedRoot);

				var applied = sut.ApplyTemplate();

				Assert.IsTrue(applied);
				Assert.IsNotNull(sut.TemplatedRoot);
				Assert.AreSame(templatedRoot, sut.TemplatedRoot);
			}
			finally
			{
				FeatureConfiguration.Control.UseLegacyLazyApplyTemplate = current;
			}
		}

		[TestMethod]
		public void When_ManuallyApplyTemplate_OutOfVisualTree()
		{
			var current = FeatureConfiguration.Control.UseLegacyLazyApplyTemplate;
			try
			{
				FeatureConfiguration.Control.UseLegacyLazyApplyTemplate = true;
				var templatedRoot = default(UIElement);
				var sut = new MyControl
				{
					Template = new ControlTemplate(() => templatedRoot = new Grid())
				};

				Assert.IsNull(sut.TemplatedRoot);
				Assert.IsNull(templatedRoot);

				var applied = sut.ApplyTemplate();

				Assert.IsTrue(applied);
				Assert.IsNotNull(sut.TemplatedRoot);
				Assert.AreSame(templatedRoot, sut.TemplatedRoot);
			}
			finally
			{
				FeatureConfiguration.Control.UseLegacyLazyApplyTemplate = current;
			}
		}

		[TestMethod]
		public void When_GetTemplateChild_Name_Only_In_Content()
		{
			//Arrange
			var namedInContent = new Border { Name = "Part" };
			var content = new Grid();
			content.Children.Add(namedInContent);

			var sut = new ContentControl { Content = content };

			//Act
			var found = sut.GetTemplateChild("Part");

			//Assert
			Assert.IsNull(found, "GetTemplateChild must only look inside the control template, never in the content.");
		}

		[TestMethod]
		public void When_GetTemplateChild_Name_In_Template()
		{
			//Arrange
			var templatePart = default(Border);
			var sut = new ContentControl
			{
				Template = new ControlTemplate(() =>
				{
					var root = new Grid();
					templatePart = new Border { Name = "Part" };
					root.Children.Add(templatePart);
					return root;
				}),
			};

			//Act
			sut.ApplyTemplate();
			var found = sut.GetTemplateChild("Part");

			//Assert
			Assert.IsNotNull(templatePart);
			Assert.AreSame(templatePart, found);
		}

		public partial class MyControl : Control
		{
		}
	}
}
