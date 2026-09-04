using Microsoft.UI.Xaml.Automation.Peers;

namespace CodeBrix.Platform.UI.CommandBar.Automation;

/// <summary>
/// Exposes a <see cref="ToolBarGroup"/> to assistive technology as a group of related items.
/// </summary>
/// <remarks>
/// A group is not a stop of its own for the keyboard, and it is not one for a screen reader
/// either: it reports <see cref="AutomationControlType.Group"/> so its items are announced as
/// belonging together, and the items keep their own names.
/// </remarks>
public partial class ToolBarGroupAutomationPeer : FrameworkElementAutomationPeer
{
	/// <summary>Initializes a peer for <paramref name="owner"/>.</summary>
	/// <param name="owner">The group this peer speaks for.</param>
	public ToolBarGroupAutomationPeer(ToolBarGroup owner)
		: base(owner)
	{
	}

	/// <inheritdoc />
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

	/// <inheritdoc />
	protected override string GetClassNameCore() => nameof(ToolBarGroup);
}
