using Microsoft.UI.Xaml.Automation.Peers;

namespace CodeBrix.Platform.UI.CommandBar.Automation;

/// <summary>
/// Exposes a <see cref="ToolBarTray"/> to assistive technology as the group its bars belong to.
/// </summary>
/// <remarks>
/// The tray is a container, not a control: it reports <see cref="AutomationControlType.Group"/> so
/// a screen reader announces the bars inside it as belonging together, and leaves each bar to
/// announce itself.
/// </remarks>
public partial class ToolBarTrayAutomationPeer : FrameworkElementAutomationPeer
{
	/// <summary>Initializes a peer for <paramref name="owner"/>.</summary>
	/// <param name="owner">The tray this peer speaks for.</param>
	public ToolBarTrayAutomationPeer(ToolBarTray owner)
		: base(owner)
	{
	}

	/// <inheritdoc />
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

	/// <inheritdoc />
	protected override string GetClassNameCore() => nameof(ToolBarTray);
}
