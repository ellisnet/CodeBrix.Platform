using Windows.Foundation;

#if HAS_CODEBRIX_WINUI
using Microsoft.UI.Input;
#else
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Input
{
	partial class PointerRoutedEventArgs
	{
		public PointerPoint GetCurrentPoint(UIElement relativeTo)
		{
			throw new global::System.NotImplementedException("The member PointerPoint PointerRoutedEventArgs.GetCurrentPoint(UIElement relativeTo) is not implemented in CodeBrix.");
		}
	}
}
