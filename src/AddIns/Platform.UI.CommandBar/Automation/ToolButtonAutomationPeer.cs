using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Exposes a <see cref="ToolButton"/> to accessibility tools and UI automation.
/// </summary>
/// <remarks>
/// The name the peer reports is the composed one - the button's label, its shortcut, and the bar
/// it belongs to - so a screen reader describes an icon-only button as fully as a labelled one.
/// That is what makes the default icon-only bar accessible rather than merely compact.
/// </remarks>
public partial class ToolButtonAutomationPeer : ButtonBaseAutomationPeer, IInvokeProvider
{
	/// <summary>Initializes a peer for the given button.</summary>
	/// <param name="owner">The button this peer describes.</param>
	public ToolButtonAutomationPeer(ToolButton owner) : base(owner)
	{
	}

	/// <summary>Gets the button this peer describes.</summary>
	protected ToolButton OwnerButton => (ToolButton)Owner;

	/// <summary>
	/// Clicks the button on an automation client's behalf.
	/// </summary>
	/// <remarks>
	/// The same click a pointer makes: the command runs, and both click events are raised.
	/// </remarks>
	public void Invoke()
	{
		if (IsEnabled())
		{
			OwnerButton.PerformClick();
		}
	}

	/// <inheritdoc/>
	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	/// <inheritdoc/>
	protected override string GetClassNameCore() => nameof(ToolButton);

	/// <inheritdoc/>
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

	/// <inheritdoc/>
	protected override string GetNameCore()
	{
		//An automation name the application set itself is the last word: it knows something the
		//composed name does not.
		var explicitName = base.GetNameCore();

		return string.IsNullOrWhiteSpace(explicitName)
			? OwnerButton.AccessibleName ?? string.Empty
			: explicitName;
	}

	/// <inheritdoc/>
	protected override string GetHelpTextCore()
	{
		var explicitHelp = base.GetHelpTextCore();

		return string.IsNullOrWhiteSpace(explicitHelp)
			? OwnerButton.ResolvedDescription ?? string.Empty
			: explicitHelp;
	}
}
