using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CodeBrix.Platform.Foundation.Extensibility;

#if HAS_CODEBRIX_WINUI
using CommunityToolkit.WinUI.Lottie;
#else
using Microsoft.Toolkit.Uwp.UI.Lottie;
#endif

#pragma warning disable 105
using Microsoft.UI.Xaml.Controls;
#pragma warning restore 105

[assembly: ApiExtension(typeof(ILottieVisualSourceProvider), typeof(CodeBrix.Platform.UI.Lottie.LottieVisualSourceProvider))]

namespace CodeBrix.Platform.UI.Lottie //Was previously: Uno.UI.Lottie
{
	public class LottieVisualSourceProvider : ILottieVisualSourceProvider
	{
		public LottieVisualSourceProvider(object owner)
		{
		}

		public IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile) => new LottieVisualSource { UriSource = sourceFile };

		public IThemableAnimatedVisualSource CreateThemableFromLottieAsset(Uri sourceFile) => new ThemableLottieVisualSource { UriSource = sourceFile };

		public bool TryCreateThemableFromAnimatedVisualSource(IAnimatedVisualSource animatedVisualSource, out IThemableAnimatedVisualSource? themableAnimatedVisualSource)
		{
			themableAnimatedVisualSource = default;
			if (animatedVisualSource is ThemableLottieVisualSource themable)
			{
				themableAnimatedVisualSource = themable;
				return true;
			}

			if (animatedVisualSource is LottieVisualSource lottieVisualSource)
			{
				themableAnimatedVisualSource = CreateThemableFromLottieAsset(lottieVisualSource.UriSource);
				return true;
			}

			return false;
		}
	}
}
