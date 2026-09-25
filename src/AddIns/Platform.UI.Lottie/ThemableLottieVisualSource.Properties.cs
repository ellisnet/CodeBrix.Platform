using CodeBrix.Platform.UI.Lottie.Engine;
using Windows.UI;

#if HAS_CODEBRIX_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	partial class ThemableLottieVisualSource
	{
		//(The bindings live in the theming engine, Engine/LottieColorTheme, as packed ARGB - WPE1 C9.)
		public void SetColorThemeProperty(string propertyName, Color? color)
		{
			_theme.SetColor(propertyName, color is { } c ? LottieColorTheme.ToArgb(c.A, c.R, c.G, c.B) : null);

			if (!_theme.HasDocument)
			{
				return; // no document to change yet
			}

			if (_theme.ApplyProperties())
			{
				NotifyCallback();
			}
		}

		public Color? GetColorThemeProperty(string propertyName)
		{
			if (_theme.GetColor(propertyName) is { } argb)
			{
				return Color.FromArgb(LottieColorTheme.Alpha(argb), LottieColorTheme.Red(argb), LottieColorTheme.Green(argb), LottieColorTheme.Blue(argb));
			}

			return default;
		}

	}
}
