#nullable enable

using Windows.Foundation;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// The framework-only bridge between the XAML/WinRT types and the shared text engine's own types
/// (<c>TextEngineTypes.skia.cs</c>, WPE1 C5). Every enum shares its underlying values with the engine's, so each
/// conversion is a cast; the geometry structs share the float storage, so each conversion is exact.
/// </summary>
internal static class TextEngineXamlConversions
{
	internal static EngineFlowDirection ToEngine(this FlowDirection value) => (EngineFlowDirection)value;

	internal static FlowDirection ToXaml(this EngineFlowDirection value) => (FlowDirection)value;

	internal static EngineTextAlignment ToEngine(this TextAlignment value) => (EngineTextAlignment)value;

	internal static EngineTextWrapping ToEngine(this Microsoft.UI.Xaml.TextWrapping value) => (EngineTextWrapping)value;

	internal static EngineLineStackingStrategy ToEngine(this LineStackingStrategy value) => (EngineLineStackingStrategy)value;

	internal static EngineFontStretch ToEngine(this FontStretch value) => (EngineFontStretch)value;

	internal static EngineFontStyle ToEngine(this FontStyle value) => (EngineFontStyle)value;

	internal static EngineSize ToEngine(this Size value) => new(value.Width, value.Height);

	internal static Size ToXaml(this EngineSize value) => new(value.Width, value.Height);

	internal static EnginePoint ToEngine(this Point value) => new(value.X, value.Y);

	internal static Rect ToXaml(this EngineRect value) => new(value.X, value.Y, value.Width, value.Height);
}
