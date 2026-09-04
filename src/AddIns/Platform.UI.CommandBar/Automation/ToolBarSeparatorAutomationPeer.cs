using Microsoft.UI.Xaml.Automation.Peers;

namespace CodeBrix.Platform.UI.CommandBar.Automation;

/// <summary>
/// Exposes a <see cref="ToolBarSeparator"/> to assistive technology as a separator.
/// </summary>
/// <remarks>
/// A separator carries no name and is not focusable, but announcing it as a separator rather than
/// as an unnamed element is what lets a screen reader say where one run of commands ends and the
/// next begins.
/// </remarks>
public partial class ToolBarSeparatorAutomationPeer : FrameworkElementAutomationPeer
{
	/// <summary>Initializes a peer for <paramref name="owner"/>.</summary>
	/// <param name="owner">The separator this peer speaks for.</param>
	public ToolBarSeparatorAutomationPeer(ToolBarSeparator owner)
		: base(owner)
	{
	}

	/// <inheritdoc />
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Separator;

	/// <inheritdoc />
	protected override string GetClassNameCore() => nameof(ToolBarSeparator);
}
