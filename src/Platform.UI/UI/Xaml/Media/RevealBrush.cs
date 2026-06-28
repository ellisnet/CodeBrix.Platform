namespace Microsoft.UI.Xaml.Media;

public partial class RevealBrush : XamlCompositionBrushBase
{
	public RevealBrush()
	{

	}

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public global::Windows.UI.Color Color
	{
		get
		{
			return (global::Windows.UI.Color)this.GetValue(ColorProperty);
		}
		set
		{
			this.SetValue(ColorProperty, value);
		}
	}

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public static global::Microsoft.UI.Xaml.DependencyProperty ColorProperty { get; } =
	Microsoft.UI.Xaml.DependencyProperty.Register(
		nameof(Color), typeof(global::Windows.UI.Color),
		typeof(global::Microsoft.UI.Xaml.Media.RevealBrush),
		new FrameworkPropertyMetadata(default(global::Windows.UI.Color)));
}
