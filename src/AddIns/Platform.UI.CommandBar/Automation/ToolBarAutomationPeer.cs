using Microsoft.UI.Xaml.Automation.Peers;

namespace CodeBrix.Platform.UI.CommandBar.Automation;

/// <summary>
/// Exposes a <see cref="ToolBar"/> to assistive technology as a tool bar.
/// </summary>
/// <remarks>
/// The bar reports the <see cref="AutomationControlType.ToolBar"/> control type, which is what
/// tells a screen reader to announce "toolbar" and to offer its items as a group rather than as
/// loose buttons, and its name is the bar's <see cref="ToolBar.Title"/>.
/// </remarks>
public partial class ToolBarAutomationPeer : ItemsControlAutomationPeer
{
	/// <summary>Initializes a peer for <paramref name="owner"/>.</summary>
	/// <param name="owner">The bar this peer speaks for.</param>
	public ToolBarAutomationPeer(ToolBar owner)
		: base(owner)
	{
	}

	/// <inheritdoc />
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;

	/// <inheritdoc />
	protected override string GetClassNameCore() => nameof(ToolBar);

	/// <inheritdoc />
	protected override string GetNameCore()
	{
		var name = base.GetNameCore();
		if (!string.IsNullOrEmpty(name))
		{
			return name;
		}

		return Owner is ToolBar bar ? bar.Title ?? string.Empty : string.Empty;
	}
}
