using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// A tool bar button that stays pressed: a two-state switch such as a magnifier or a "show
/// invisibles" toggle.
/// </summary>
/// <remarks>
/// The checked state is the VIEW's, bound two-way to whatever property of the view model owns it;
/// there is deliberately no rule that infers it from the command. Three-state is not offered - a
/// tool bar toggle that can be neither on nor off is not a thing desktop tool bars do.
/// </remarks>
public partial class ToolToggleButton : ToolButton
{
	/// <summary>Initializes a new toggle button.</summary>
	public ToolToggleButton()
	{
		DefaultStyleKey = typeof(ToolToggleButton);

		Click += OnToggleClick;
	}

	/// <summary>
	/// Occurs after <see cref="IsChecked"/> has changed, whoever changed it.
	/// </summary>
	public event TypedEventHandler<ToolToggleButton, RoutedEventArgs>? IsCheckedChanged;

	/// <summary>
	/// Identifies the <see cref="IsChecked"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty IsCheckedProperty =
		DependencyProperty.Register(
			nameof(IsChecked),
			typeof(bool),
			typeof(ToolToggleButton),
			new FrameworkPropertyMetadata(false, OnIsCheckedChanged));

	/// <summary>
	/// Gets or sets whether the button is switched on.
	/// </summary>
	/// <remarks>
	/// Bind this two-way - <c>IsChecked="{Binding Magnifier, Mode=TwoWay}"</c> - and the view model
	/// and the button stay in step in both directions: a click writes the new value back, and a
	/// change made anywhere else in the application shows up on the button.
	/// </remarks>
	public bool IsChecked
	{
		get => (bool)GetValue(IsCheckedProperty);
		set => SetValue(IsCheckedProperty, value);
	}

	/// <inheritdoc/>
	protected override AutomationPeer OnCreateAutomationPeer() => new ToolToggleButtonAutomationPeer(this);

	/// <inheritdoc/>
	private protected override void UpdateVisualStates(bool useTransitions)
	{
		base.UpdateVisualStates(useTransitions);

		VisualStateManager.GoToState(this, IsChecked ? "Checked" : "Unchecked", useTransitions);
	}

	/// <summary>Flips the checked state, because a click on a toggle is what changes it.</summary>
	/// <param name="sender">The button.</param>
	/// <param name="args">The click arguments.</param>
	private void OnToggleClick(object sender, RoutedEventArgs args) => IsChecked = !IsChecked;

	/// <summary>Reacts to the checked state changing, from any source.</summary>
	/// <param name="sender">The toggle button.</param>
	/// <param name="args">The change.</param>
	private static void OnIsCheckedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var button = (ToolToggleButton)sender;

		button.UpdateVisualStates(useTransitions: true);
		button.IsCheckedChanged?.Invoke(button, new RoutedEventArgs());

		if (FrameworkElementAutomationPeer.FromElement(button) is ToolToggleButtonAutomationPeer peer)
		{
			peer.RaiseToggleStateChanged((bool)args.OldValue, (bool)args.NewValue);
		}
	}
}
