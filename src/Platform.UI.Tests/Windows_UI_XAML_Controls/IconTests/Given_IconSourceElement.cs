using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_XAML_Controls.IconTests
{
	[TestClass]
	public class Given_IconSourceElement
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_FontIconSource()
		{
			//Arrange
			var element = new IconSourceElement();

			//Act
			element.IconSource = new FontIconSource { Glyph = "A" };

			//Assert
			var icon = ChildIcon(element) as FontIcon;
			Assert.IsNotNull(icon, "A FontIconSource must produce a FontIcon.");
			Assert.AreEqual("A", icon.Glyph);
		}

		[TestMethod]
		public void When_SymbolIconSource()
		{
			//Arrange
			var element = new IconSourceElement();

			//Act
			element.IconSource = new SymbolIconSource { Symbol = Symbol.Save };

			//Assert
			var icon = ChildIcon(element) as SymbolIcon;
			Assert.IsNotNull(icon, "A SymbolIconSource must produce a SymbolIcon.");
			Assert.AreEqual(Symbol.Save, icon.Symbol);
		}

		[TestMethod]
		public void When_ImageIconSource()
		{
			//Arrange
			// ImageIconSource is one of the framework's own icon sources, and it is NOT one of the
			// four this wrapper builds by hand - so before the fallback it drew nothing at all.
			var element = new IconSourceElement();

			//Act
			element.IconSource = new ImageIconSource
			{
				ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/icon.png")),
			};

			//Assert
			Assert.IsInstanceOfType(ChildIcon(element), typeof(ImageIcon),
				"An ImageIconSource must produce the element it creates for itself.");
		}

		[TestMethod]
		public void When_A_Source_Outside_The_Frameworks_Own()
		{
			//Arrange
			// The shape an add-in or an application uses: an icon source of its own, whose
			// properties the wrapper cannot know, so it asks the source for an element.
			var element = new IconSourceElement();
			var source = new TestIconSource();

			//Act
			element.IconSource = source;

			//Assert
			var icon = ChildIcon(element);
			Assert.IsNotNull(icon, "A third-party icon source must reach the wrapper's child.");
			Assert.AreSame(source.LastCreated, icon,
				"The element the source created must be the one the wrapper shows.");
		}

		[TestMethod]
		public void When_The_Source_Carries_A_Foreground()
		{
			//Arrange
			var element = new IconSourceElement();
			var brush = new SolidColorBrush(Microsoft.UI.Colors.Red);

			//Act
			element.IconSource = new TestIconSource { Foreground = brush };

			//Assert
			Assert.AreSame(brush, ChildIcon(element).Foreground,
				"A third-party source's Foreground must reach the element it created.");
		}

		[TestMethod]
		public void When_The_Source_Is_Replaced()
		{
			//Arrange
			var element = new IconSourceElement();
			element.IconSource = new TestIconSource();
			var first = ChildIcon(element);

			//Act
			element.IconSource = new SymbolIconSource { Symbol = Symbol.Home };

			//Assert
			Assert.IsInstanceOfType(ChildIcon(element), typeof(SymbolIcon));
			Assert.AreNotSame(first, ChildIcon(element), "The old element must be dropped.");
		}

		[TestMethod]
		public void When_The_Source_Is_Cleared()
		{
			//Arrange
			var element = new IconSourceElement();
			element.IconSource = new TestIconSource();

			//Act
			element.IconSource = null;

			//Assert
			Assert.IsNull(ChildIcon(element), "Clearing the source must leave no icon behind.");
		}

		/// <summary>
		/// The icon element the wrapper is showing: it sits inside the root grid every icon element
		/// builds for itself, so the whole subtree is searched rather than the direct children.
		/// </summary>
		/// <param name="element">The wrapper.</param>
		/// <returns>The icon element, or null when the wrapper built none.</returns>
		private static IconElement ChildIcon(IconSourceElement element)
			=> Descendants(element).OfType<IconElement>().FirstOrDefault();

		private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
		{
			var count = VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);

				yield return child;

				foreach (var deeper in Descendants(child))
				{
					yield return deeper;
				}
			}
		}

		/// <summary>An icon source outside the framework's own four, as an add-in supplies.</summary>
		private sealed class TestIconSource : IconSource
		{
			public IconElement LastCreated { get; private set; }

			protected override IconElement CreateIconElementCore()
			{
				LastCreated = new FontIcon { Glyph = "B" };

				return LastCreated;
			}
		}
	}
}
