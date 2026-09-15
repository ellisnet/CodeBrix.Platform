using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Exposes a <see cref="ToolToggleButton"/> to accessibility tools and UI automation.
/// </summary>
/// <remarks>
/// The toggle pattern is what tells a screen reader that this button has a state rather than only
/// an action, so "Magnifier, on" can be announced instead of just "Magnifier".
/// </remarks>
public partial class ToolToggleButtonAutomationPeer : ToolButtonAutomationPeer, IToggleProvider
{
	/// <summary>Initializes a peer for the given toggle button.</summary>
	/// <param name="owner">The toggle button this peer describes.</param>
	public ToolToggleButtonAutomationPeer(ToolToggleButton owner) : base(owner)
	{
	}

	/// <summary>Gets the toggle state the button is in.</summary>
	public ToggleState ToggleState => OwnerToggleButton.IsChecked ? ToggleState.On : ToggleState.Off;

	/// <summary>Gets the toggle button this peer describes.</summary>
	private ToolToggleButton OwnerToggleButton => (ToolToggleButton)Owner;

	/// <summary>Switches the button on or off, as a click would.</summary>
	public void Toggle()
	{
		if (IsEnabled())
		{
			OwnerToggleButton.PerformClick();
		}
	}

	/// <summary>
	/// Tells automation clients that the toggle state changed.
	/// </summary>
	/// <param name="oldValue">The state before the change.</param>
	/// <param name="newValue">The state after the change.</param>
	internal void RaiseToggleStateChanged(bool oldValue, bool newValue)
	{
		if (oldValue != newValue)
		{
			RaisePropertyChangedEvent(
				TogglePatternIdentifiers.ToggleStateProperty,
				oldValue ? ToggleState.On : ToggleState.Off,
				newValue ? ToggleState.On : ToggleState.Off);
		}
	}

	/// <inheritdoc/>
	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Toggle)
		{
			return this;
		}

		//A toggle has a STATE, not an action: "on" and "off" are what an automation client sets,
		//and Invoke has no answer to which of the two it would leave the button in. WinUI's own
		//ToggleButtonAutomationPeer advertises Toggle alone for that reason, and the drop-down
		//peer here already drops a pattern its button cannot honour. The base peer implements
		//IInvokeProvider and would otherwise hand it out for every toggle in a bar.
		if (patternInterface == PatternInterface.Invoke)
		{
			return null!;
		}

		return base.GetPatternCore(patternInterface);
	}

	/// <inheritdoc/>
	protected override string GetClassNameCore() => nameof(ToolToggleButton);
}
