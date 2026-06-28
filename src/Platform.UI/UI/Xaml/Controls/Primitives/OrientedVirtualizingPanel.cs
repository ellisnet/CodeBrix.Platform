using System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

[global::CodeBrix.Platform.NotImplemented]
public partial class OrientedVirtualizingPanel
{
	public OrientedVirtualizingPanel()
	{
	}

	private protected override VirtualizingPanelLayout GetLayouterCore()
	{
		throw new NotSupportedException(
			$"{GetType().Name} is not supported in CodeBrix Platform yet. " +
			"Use a non-virtualized panel (e.g. ItemsStackPanel) instead.");
	}
}
