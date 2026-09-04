using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Exposes a <see cref="ToolDropDownButton"/> to accessibility tools and UI automation.
/// </summary>
/// <remarks>
/// A drop-down button offers two patterns because it does two things: invoking it does what its
/// command does, and expanding it opens its menu. Which of the two an automation client should
/// reach for is the same question a user faces, and the answer is the button's popup mode.
/// </remarks>
public partial class ToolDropDownButtonAutomationPeer : ToolButtonAutomationPeer, IExpandCollapseProvider
{
	/// <summary>Initializes a peer for the given drop-down button.</summary>
	/// <param name="owner">The drop-down button this peer describes.</param>
	public ToolDropDownButtonAutomationPeer(ToolDropDownButton owner) : base(owner)
	{
	}

	/// <summary>Gets whether the flyout is open.</summary>
	public ExpandCollapseState ExpandCollapseState
		=> OwnerDropDownButton.IsFlyoutOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

	/// <summary>Gets the drop-down button this peer describes.</summary>
	private ToolDropDownButton OwnerDropDownButton => (ToolDropDownButton)Owner;

	/// <summary>Opens the button's flyout.</summary>
	/// <exception cref="InvalidOperationException">The button has no flyout to open.</exception>
	public void Expand()
	{
		if (OwnerDropDownButton.Flyout is null)
		{
			throw new InvalidOperationException("This drop-down button has no flyout to expand.");
		}

		OwnerDropDownButton.OpenFlyout();
	}

	/// <summary>Closes the button's flyout.</summary>
	public void Collapse() => OwnerDropDownButton.CloseFlyout();

	/// <inheritdoc/>
	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.ExpandCollapse)
		{
			return this;
		}

		//A button whose whole face opens a menu has no command to invoke, and saying otherwise
		//would have an automation client clicking a button that does nothing.
		if (patternInterface == PatternInterface.Invoke && OwnerDropDownButton.PopupMode == PopupMode.Instant)
		{
			return null!;
		}

		return base.GetPatternCore(patternInterface);
	}

	/// <inheritdoc/>
	protected override string GetClassNameCore() => nameof(ToolDropDownButton);

	/// <inheritdoc/>
	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.SplitButton;
}
