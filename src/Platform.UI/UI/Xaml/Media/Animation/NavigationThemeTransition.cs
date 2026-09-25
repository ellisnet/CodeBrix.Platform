using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Media.Animation;

[ContentProperty(Name = nameof(DefaultNavigationTransitionInfo))]
#if IS_UNIT_TESTS || __CROSSRUNTIME__
[global::CodeBrix.Platform.NotImplemented]
#endif
public partial class NavigationThemeTransition : Transition
{
}
