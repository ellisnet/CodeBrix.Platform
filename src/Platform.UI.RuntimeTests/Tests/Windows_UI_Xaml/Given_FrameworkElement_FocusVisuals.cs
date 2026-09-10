using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_UI_Xaml //Was previously: Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_FrameworkElement_FocusVisuals
	{
		//TEST PARTLY REPLACED - by src/UIReqs/Platform.UI.Core/Features/ThemeFocus/Focus.feature
		[TestMethod]
		[RunsOnUIThread]
		public void When_PrimaryThickness_Default()
		{
			var element = new PlainFrameworkElement();
			var actualThickness = element.FocusVisualPrimaryThickness;
			var expectedThickness = ThicknessFromUniformLength(2);
			Assert.AreEqual(expectedThickness, actualThickness);
		}

		//TEST PARTLY REPLACED - by src/UIReqs/Platform.UI.Core/Features/ThemeFocus/Focus.feature
		[TestMethod]
		[RunsOnUIThread]
		public void When_SecondaryThickness_Default()
		{
			var element = new PlainFrameworkElement();
			var actualThickness = element.FocusVisualSecondaryThickness;
			var expectedThickness = ThicknessFromUniformLength(1);
			Assert.AreEqual(expectedThickness, actualThickness);
		}

		//TEST PARTLY REPLACED - by src/UIReqs/Platform.UI.Core/Features/ThemeFocus/Focus.feature
		[TestMethod]
		[RunsOnUIThread]
		public void When_Margin_Default()
		{
			var element = new PlainFrameworkElement();
			var actualMargin = element.FocusVisualMargin;
			var expectedMargin = ThicknessFromUniformLength(0);
			Assert.AreEqual(expectedMargin, actualMargin);
		}

		//TEST PARTLY REPLACED - by src/UIReqs/Platform.UI.Core/Features/ThemeFocus/Focus.feature
		[TestMethod]
		[RunsOnUIThread]
		public void When_PrimaryBrush_Default()
		{
			var element = new PlainFrameworkElement();
			var expectedBrush = Application.Current.Resources["SystemControlFocusVisualPrimaryBrush"];
			var actualBrush = element.FocusVisualPrimaryBrush;
			Assert.AreEqual(expectedBrush, actualBrush);
		}

		//TEST PARTLY REPLACED - by src/UIReqs/Platform.UI.Core/Features/ThemeFocus/Focus.feature
		[TestMethod]
		[RunsOnUIThread]
		public void When_SecondaryBrush_Default()
		{
			var element = new PlainFrameworkElement();
			var expectedBrush = Application.Current.Resources["SystemControlFocusVisualSecondaryBrush"];
			var actualBrush = element.FocusVisualSecondaryBrush;
			Assert.AreEqual(expectedBrush, actualBrush);
		}

		private static Thickness ThicknessFromUniformLength(double uniformLength) =>
			new Thickness(uniformLength);
	}

	public partial class PlainFrameworkElement : FrameworkElement
	{
	}
}
