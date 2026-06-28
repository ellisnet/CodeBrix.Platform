using Windows.Foundation;

#if IS_CODEBRIX_COMPOSITION
namespace CodeBrix.Platform.UI.Composition; //Was previously: Uno.UI.Composition
#else
namespace Microsoft.UI.Xaml;
#endif

internal partial record struct FullCornerRadius
(
	NonUniformCornerRadius Outer,
	NonUniformCornerRadius Inner
)
{
	public static FullCornerRadius None { get; }

	public bool IsEmpty => Outer.IsEmpty && Inner.IsEmpty;
}
