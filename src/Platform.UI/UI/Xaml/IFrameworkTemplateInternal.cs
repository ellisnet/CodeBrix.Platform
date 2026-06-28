#nullable enable

#if false
using View = Android.Views.View;
#elif false
using View = UIKit.UIView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif


namespace Microsoft.UI.Xaml;

internal interface IFrameworkTemplateInternal
{
	View? LoadContent(DependencyObject? templatedParent);
}
