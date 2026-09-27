namespace Microsoft.UI.Xaml.Controls;

// VirtualizingStackPanel is not implemented (only internal MUX helpers exist, in VirtualizingStackPanel.mux.partial.cs).
// Because a hand-written part exists, the API sync generator no longer emits the type-level NotImplemented marker and the
// constructor stub, so both are declared here, unchanged.

/// <summary>
/// Arranges data on a single line that can be oriented horizontally or vertically (not implemented).
/// </summary>
[global::CodeBrix.Platform.NotImplemented]
public partial class VirtualizingStackPanel
{
	/// <summary>
	/// Initializes a new instance of the <see cref="VirtualizingStackPanel"/> class (not implemented).
	/// </summary>
	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__", "__CODEBRIX_CORE__")]
	public VirtualizingStackPanel()
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.VirtualizingStackPanel", "VirtualizingStackPanel.VirtualizingStackPanel()");
	}
}
