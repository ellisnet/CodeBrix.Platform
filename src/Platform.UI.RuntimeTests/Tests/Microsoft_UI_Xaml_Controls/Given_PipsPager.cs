using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using CodeBrix.Platform.UI.RuntimeTests.Helpers;

#if HAS_CODEBRIX && !HAS_CODEBRIX_WINUI
using Windows.UI.Xaml.Controls;
#endif

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls; //Was previously: Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls

[TestClass]
public partial class Given_PipsPager
{
	[TestMethod]
	[RunsOnUIThread]
#if false
	[Ignore("RenderTargetBitmap is not implemented on WASM.")]
#else
	[Ignore("Fails even on Windows, very flaky on CodeBrix.")] // Flaky #9080
#endif
	public async Task When_MaxVisiblePips_GreaterThan_NumberOfPages_Horizontal()
	{
		var SUT = new PipsPager
		{
			NumberOfPages = 7,
			MaxVisiblePips = 5
		};

		await UITestHelper.Load(SUT);

		var initialScreenshot = await UITestHelper.ScreenShot(SUT);

		var color = initialScreenshot.GetPixel(initialScreenshot.Width - 5, initialScreenshot.Height / 2);

		SUT.SelectedPageIndex = 3;
		await TestServices.WindowHelper.WaitForIdle();

		var scrolledScreenshot = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(scrolledScreenshot, scrolledScreenshot.Width - 5, scrolledScreenshot.Height / 2, color);
	}

	[TestMethod]
	[RunsOnUIThread]
#if false
	[Ignore("RenderTargetBitmap is not implemented on WASM.")]
#else
	[Ignore("Very flaky on CodeBrix.")] // Flaky #9080
#endif
	public async Task When_MaxVisiblePips_GreaterThan_NumberOfPages_Vertical()
	{
		var SUT = new PipsPager
		{
			NumberOfPages = 7,
			MaxVisiblePips = 5
		};

		await UITestHelper.Load(SUT);

		var initialScreenshot = await UITestHelper.ScreenShot(SUT);

		var color = initialScreenshot.GetPixel(initialScreenshot.Width / 2, initialScreenshot.Height - 5);

		SUT.SelectedPageIndex = 3;
		await TestServices.WindowHelper.WaitForIdle();

		var scrolledScreenshot = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(scrolledScreenshot, scrolledScreenshot.Width / 2, scrolledScreenshot.Height - 5, color);
	}
}
