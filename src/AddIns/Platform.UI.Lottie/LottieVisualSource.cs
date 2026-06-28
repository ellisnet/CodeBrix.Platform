using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Data;
using CodeBrix.Platform.Extensions.Disposables;

#if HAS_CODEBRIX_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	[Bindable]
	public partial class LottieVisualSource : LottieVisualSourceBase
	{
#if !DEBUG
		protected override bool IsPayloadNeedsToBeUpdated => false; // load the animation using url if possible
#else
		protected override bool IsPayloadNeedsToBeUpdated => false;
#endif
	}
}
