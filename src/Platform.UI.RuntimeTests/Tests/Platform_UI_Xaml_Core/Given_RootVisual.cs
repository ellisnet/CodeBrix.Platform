using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Private.Infrastructure;
using CodeBrix.Platform.UI.RuntimeTests.Helpers;
using Windows.UI;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Platform_UI_Xaml_Core; //Was previously: Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core

[TestClass]
[RequiresFullWindow]
public class Given_RootVisual
{
#if HAS_CODEBRIX
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Theme_Changes()
	{
		var rootVisual = CodeBrix.Platform.UI.Xaml.Core.CoreServices.Instance.MainRootVisual;
		if (rootVisual is null)
		{
			// Ignore on Uno Islands
			return;
		}

		Assert.AreEqual(Colors.White, ((SolidColorBrush)rootVisual.Background).Color);

		using (ThemeHelper.UseDarkTheme())
		{
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(Colors.Black, ((SolidColorBrush)rootVisual.Background).Color);
		}

		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(Colors.White, ((SolidColorBrush)rootVisual.Background).Color);
	}
#endif
}
