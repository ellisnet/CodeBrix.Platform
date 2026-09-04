using System;
using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The ordinary button of a tool bar: an icon, a text label, or both, wired to a command.
/// </summary>
/// <remarks>
/// <para>
/// The button binds to any <c>ICommand</c>, and its enabled state follows that command's
/// <c>CanExecute</c> - including a later <c>CanExecuteChanged</c> - unless the application has set
/// <c>IsEnabled</c> to false itself, which always wins. Bound to a <c>XamlUICommand</c> or a
/// <c>StandardUICommand</c>, it also takes that command's label, icon, description and keyboard
/// accelerators for anything it has not set for itself, so one command object can drive the tool
/// bar button, the menu item and the accelerator together.
/// </para>
/// <para>
/// How much of the button is shown - icon, text or both, and where the text sits - comes from the
/// inherited attached properties on <see cref="ToolBarProperties"/>, which the bar normally sets
/// once for every item. An icon-only button still carries its text: in the composed tooltip, and
/// in the name a screen reader reads.
/// </para>
/// </remarks>
public partial class ToolButton : ButtonBase
{
	/// <summary>The accelerators this button copied from a bound command, so it can take them back.</summary>

	/// <summary>The tooltip text this button last composed, so an application-set tooltip is left alone.</summary>
	private object? _composedToolTip;

	/// <summary>The icon source the current icon element was built from.</summary>
	private IconSource? _shownIconSource;

	/// <summary>Initializes a new tool bar button.</summary>
	public ToolButton()
	{
		DefaultStyleKey = typeof(ToolButton);

		Click += OnToolButtonClick;

		//The four bar-level settings are INHERITED, so they arrive without anyone setting them on
		//this button: watching the property is the only way to notice a bar changing its mind.
		RegisterPropertyChangedCallback(ToolBarProperties.IconSizeProperty, OnPresentationInputChanged);
		RegisterPropertyChangedCallback(ToolBarProperties.LabelModeProperty, OnPresentationInputChanged);
		RegisterPropertyChangedCallback(ToolBarProperties.LabelPositionProperty, OnPresentationInputChanged);
		RegisterPropertyChangedCallback(ToolBarProperties.ShowToolTipsProperty, OnPresentationInputChanged);
		RegisterPropertyChangedCallback(CommandProperty, OnCommandInputChanged);
		RegisterPropertyChangedCallback(ContentProperty, OnPresentationInputChanged);

		RegisterPropertyChangedCallback(IsEnabledProperty, OnVisualStateInputChanged);
		RegisterPropertyChangedCallback(IsPressedProperty, OnVisualStateInputChanged);
		RegisterPropertyChangedCallback(IsPointerOverProperty, OnVisualStateInputChanged);

		UpdatePresentation();
	}

	/// <summary>
	/// Occurs when the button is clicked, carrying the modifier keys that were held down.
	/// </summary>
	/// <remarks>
	/// Raised in addition to the ordinary Click event, immediately after it, for every route that
	/// clicks the button - pointer, keyboard and automation.
	/// </remarks>
	public event TypedEventHandler<ToolButton, ClickWithModifiersEventArgs>? ClickWithModifiers;

	#region Icon

	/// <summary>
	/// Identifies the <see cref="Icon"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty IconProperty =
		DependencyProperty.Register(
			nameof(Icon),
			typeof(ToolIconSource),
			typeof(ToolButton),
			new FrameworkPropertyMetadata(null, OnPresentationPropertyChanged));

	/// <summary>
	/// Gets or sets the icon the button shows.
	/// </summary>
	/// <remarks>
	/// When the button has no icon of its own and its command is a <c>XamlUICommand</c>, the
	/// command's icon source is shown instead - including the framework's own symbol and font icons,
	/// so a command written for the rest of the application still draws something here.
	/// </remarks>
	public ToolIconSource? Icon
	{
		get => (ToolIconSource?)GetValue(IconProperty);
		set => SetValue(IconProperty, value);
	}

	#endregion

	#region Text

	/// <summary>
	/// Identifies the <see cref="Text"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty TextProperty =
		DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(ToolButton),
			new FrameworkPropertyMetadata(null, OnPresentationPropertyChanged));

	/// <summary>
	/// Gets or sets the button's label.
	/// </summary>
	/// <remarks>
	/// The label is used even when it is not drawn: an icon-only button puts it in the tooltip and
	/// reads it out to a screen reader, which is what keeps an icon-only bar usable.
	/// </remarks>
	public string? Text
	{
		get => (string?)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	#endregion

	#region Shortcut

	/// <summary>
	/// Identifies the <see cref="Shortcut"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty ShortcutProperty =
		DependencyProperty.Register(
			nameof(Shortcut),
			typeof(string),
			typeof(ToolButton),
			new FrameworkPropertyMetadata(null, OnPresentationPropertyChanged));

	/// <summary>
	/// Gets or sets the shortcut text shown in the tooltip, such as "Ctrl+S".
	/// </summary>
	/// <remarks>
	/// Set this only to override what would otherwise be worked out: a keyboard accelerator
	/// registered on the button, or the first accelerator of a bound <c>XamlUICommand</c>, is
	/// formatted and shown without anything being set here.
	/// </remarks>
	public string? Shortcut
	{
		get => (string?)GetValue(ShortcutProperty);
		set => SetValue(ShortcutProperty, value);
	}

	#endregion

	#region ShowToolTip

	/// <summary>
	/// Identifies the <see cref="ShowToolTip"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty ShowToolTipProperty =
		DependencyProperty.Register(
			nameof(ShowToolTip),
			typeof(bool?),
			typeof(ToolButton),
			new FrameworkPropertyMetadata(null, OnPresentationPropertyChanged));

	/// <summary>
	/// Gets or sets whether this button shows its composed tooltip, overriding the bar.
	/// </summary>
	/// <remarks>
	/// Null - the default - means "whatever the bar says", which is
	/// <see cref="ToolBarProperties.ShowToolTipsProperty"/>. False suppresses the tooltip on this
	/// button even in a bar that shows them; true brings it back in a bar that does not. Suppressing
	/// the tooltip does not affect the name a screen reader reads.
	/// </remarks>
	public bool? ShowToolTip
	{
		get => (bool?)GetValue(ShowToolTipProperty);
		set => SetValue(ShowToolTipProperty, value);
	}

	#endregion

	#region Template contract

	/// <summary>
	/// Identifies the <see cref="ResolvedText"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty ResolvedTextProperty =
		DependencyProperty.Register(
			nameof(ResolvedText),
			typeof(string),
			typeof(ToolButton),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets the label the button actually shows, after the command has been consulted.
	/// </summary>
	/// <remarks>
	/// Part of the control template's contract, and set by the button; setting it from outside is
	/// overwritten the next time anything it is derived from changes.
	/// </remarks>
	public string? ResolvedText => (string?)GetValue(ResolvedTextProperty);

	/// <summary>
	/// Identifies the <see cref="IconVisual"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty IconVisualProperty =
		DependencyProperty.Register(
			nameof(IconVisual),
			typeof(IconElement),
			typeof(ToolButton),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets the element that draws the button's icon, built from the icon source.
	/// </summary>
	/// <remarks>
	/// Part of the control template's contract. A fresh element is built whenever the icon source
	/// changes, because an element has one parent and cannot be shared between buttons.
	/// </remarks>
	public IconElement? IconVisual => (IconElement?)GetValue(IconVisualProperty);

	/// <summary>
	/// Identifies the <see cref="EffectiveIconSize"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty EffectiveIconSizeProperty =
		DependencyProperty.Register(
			nameof(EffectiveIconSize),
			typeof(double),
			typeof(ToolButton),
			new FrameworkPropertyMetadata(ToolBarProperties.DefaultIconSize));

	/// <summary>
	/// Gets the icon's edge length in logical pixels, as inherited from the bar or overridden here.
	/// </summary>
	public double EffectiveIconSize => (double)GetValue(EffectiveIconSizeProperty);

	/// <summary>
	/// Identifies the <see cref="IconVisibility"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty IconVisibilityProperty =
		DependencyProperty.Register(
			nameof(IconVisibility),
			typeof(Visibility),
			typeof(ToolButton),
			new FrameworkPropertyMetadata(Visibility.Visible));

	/// <summary>Gets whether the icon part of the template is shown.</summary>
	public Visibility IconVisibility => (Visibility)GetValue(IconVisibilityProperty);

	/// <summary>
	/// Identifies the <see cref="TextVisibility"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty TextVisibilityProperty =
		DependencyProperty.Register(
			nameof(TextVisibility),
			typeof(Visibility),
			typeof(ToolButton),
			new FrameworkPropertyMetadata(Visibility.Collapsed));

	/// <summary>Gets whether the text part of the template is shown.</summary>
	public Visibility TextVisibility => (Visibility)GetValue(TextVisibilityProperty);

	/// <summary>
	/// Identifies the <see cref="LabelOrientation"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty LabelOrientationProperty =
		DependencyProperty.Register(
			nameof(LabelOrientation),
			typeof(Orientation),
			typeof(ToolButton),
			new FrameworkPropertyMetadata(Orientation.Horizontal));

	/// <summary>
	/// Gets how the icon and the text are stacked: side by side, or the text under the icon.
	/// </summary>
	public Orientation LabelOrientation => (Orientation)GetValue(LabelOrientationProperty);

	#endregion

	/// <summary>
	/// Gets the tooltip text this button would show, whether or not tooltips are switched on.
	/// </summary>
	/// <remarks>
	/// Useful for a bar that wants to show the same wording somewhere else - a status line, say -
	/// while tooltips are off.
	/// </remarks>
	public string? ComposedToolTipText
		=> ToolTipComposer.Compose(ResolvedText, ResolvedShortcutText, ResolvedDescription);

	/// <summary>
	/// Gets the name a screen reader announces for this button.
	/// </summary>
	public string? AccessibleName
		=> ToolTipComposer.ComposeAccessibleName(
			ResolvedText,
			ResolvedShortcutText,
			ResolvedDescription,
			ToolCommandSupport.FindBarTitle(this));

	/// <summary>Gets whether the composed tooltip is shown, after the bar and the override are read.</summary>
	internal bool IsToolTipShown => ShowToolTip ?? ToolBarProperties.GetShowToolTips(this);

	/// <summary>Gets the shortcut text the tooltip uses.</summary>
	internal string? ResolvedShortcutText
		=> ToolCommandSupport.ResolveShortcutText(Shortcut, KeyboardAccelerators, Command);

	/// <summary>Gets the description a bound command supplies.</summary>
	internal string? ResolvedDescription => ToolCommandSupport.ResolveDescription(Command);

	/// <summary>
	/// Clicks the button as a pointer or the keyboard would, running the command and raising both
	/// click events.
	/// </summary>
	/// <remarks>
	/// This is the route an automation client takes, and the route a test takes; it deliberately
	/// goes through the same code as a real click rather than reproducing part of it.
	/// </remarks>
	internal void PerformClick() => ProgrammaticClick();

	/// <inheritdoc/>
	protected override AutomationPeer OnCreateAutomationPeer() => new ToolButtonAutomationPeer(this);

	/// <inheritdoc/>
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		UpdateVisualStates(useTransitions: false);
	}

	/// <summary>
	/// Recomputes everything the template shows and the tooltip says.
	/// </summary>
	/// <remarks>
	/// Called whenever an input to that answer changes - the button's own properties, the command,
	/// or a bar-level setting arriving through inheritance - so there is exactly one place where
	/// the presentation is decided.
	/// </remarks>
	private protected virtual void UpdatePresentation()
	{
		var labelMode = ToolBarProperties.GetLabelMode(this);
		var iconSource = ToolCommandSupport.ResolveIconSource(Icon, Command);
		var text = ToolCommandSupport.ResolveText(Text, Command, Content);

		SetValue(ResolvedTextProperty, text);
		SetValue(EffectiveIconSizeProperty, ToolBarProperties.GetIconSize(this));
		SetValue(
			LabelOrientationProperty,
			ToolBarProperties.GetLabelPosition(this) == LabelPosition.Bottom
				? Orientation.Vertical
				: Orientation.Horizontal);

		//An icon-only button with no icon would be a blank square, and a text-only button with no
		//text would be nothing at all: in both cases the other half is shown rather than an empty
		//button. That is why the modes are read together with what the button actually has.
		var hasIcon = iconSource is not null;
		var hasText = !string.IsNullOrEmpty(text);

		var showIcon = labelMode switch
		{
			LabelMode.TextOnly => hasIcon && !hasText,
			_ => hasIcon,
		};

		var showText = labelMode switch
		{
			LabelMode.IconOnly => hasText && !hasIcon,
			_ => hasText,
		};

		SetValue(IconVisibilityProperty, showIcon ? Visibility.Visible : Visibility.Collapsed);
		SetValue(TextVisibilityProperty, showText ? Visibility.Visible : Visibility.Collapsed);

		UpdateIconVisual(iconSource);
		UpdateToolTip();
	}

	/// <summary>Rebuilds the icon element when the icon source it draws has changed.</summary>
	/// <param name="iconSource">The icon source to draw, or null.</param>
	private void UpdateIconVisual(IconSource? iconSource)
	{
		if (iconSource is null)
		{
			SetValue(IconVisualProperty, null);
			_shownIconSource = null;
			return;
		}

		if (ReferenceEquals(iconSource, _shownIconSource) && IconVisual is not null)
		{
			return;
		}

		_shownIconSource = iconSource;
		SetValue(IconVisualProperty, iconSource.CreateIconElement());
	}

	/// <summary>Composes and installs - or removes - the button's tooltip.</summary>
	private void UpdateToolTip()
	{
		if (ApplicationOwnsToolTip())
		{
			return;
		}

		var text = IsToolTipShown ? ComposedToolTipText : null;

		//Clearing first drops the description binding the framework installs when a XamlUICommand
		//is bound, which would otherwise say only half of what the composed tooltip says.
		ClearValue(ToolTipService.ToolTipProperty);
		_composedToolTip = text;

		if (text is not null)
		{
			ToolTipService.SetToolTip(this, text);
		}
	}

	/// <summary>Gets whether the application set a tooltip of its own that must not be overwritten.</summary>
	/// <returns>True when the current tooltip did not come from this button or from its command.</returns>
	private bool ApplicationOwnsToolTip()
	{
		var current = ToolTipService.GetToolTip(this);

		if (current is null || ReferenceEquals(current, _composedToolTip))
		{
			return false;
		}

		//The framework's own "description of the bound command" binding is not the application
		//speaking; the composed tooltip includes that description and says more.
		var expression = GetBindingExpression(ToolTipService.ToolTipProperty);

		return expression is null || !ReferenceEquals(expression.ParentBinding?.Source, Command);
	}

	/// <summary>Moves the control to the visual state its current pointer and enabled state call for.</summary>
	/// <param name="useTransitions">Whether the change should animate.</param>
	private protected virtual void UpdateVisualStates(bool useTransitions)
	{
		var state = !IsEnabled ? "Disabled"
			: IsPressed ? "Pressed"
			: IsPointerOver ? "PointerOver"
			: "Normal";

		VisualStateManager.GoToState(this, state, useTransitions);
	}

	/// <summary>Raises <see cref="ClickWithModifiers"/> with the keys held down at this moment.</summary>
	/// <param name="sender">The button that was clicked.</param>
	/// <param name="args">The click arguments, which carry no modifier information of their own.</param>
	private void OnToolButtonClick(object sender, RoutedEventArgs args)
	{
		var handler = ClickWithModifiers;

		if (handler is null)
		{
			return;
		}

		//Read at the click rather than remembered from the last key event: that is the whole point
		//of the event, and it is what makes a Shift-click reliable on every head.
		handler(this, new ClickWithModifiersEventArgs(ToolCommandSupport.ModifierProbe()));
	}

	/// <summary>Handles a change to a property the presentation is derived from.</summary>
	/// <param name="sender">The button.</param>
	/// <param name="property">The property that changed.</param>
	private void OnPresentationInputChanged(DependencyObject sender, DependencyProperty property)
		=> UpdatePresentation();

	/// <summary>Handles a change to a property the visual state is derived from.</summary>
	/// <param name="sender">The button.</param>
	/// <param name="property">The property that changed.</param>
	private void OnVisualStateInputChanged(DependencyObject sender, DependencyProperty property)
		=> UpdateVisualStates(useTransitions: true);

	/// <summary>Handles a change of the bound command.</summary>
	/// <param name="sender">The button.</param>
	/// <param name="property">The Command property.</param>
	/// <remarks>
	/// This runs after the framework has done its own work for the new command, so anything set
	/// here has the last word - which is what "the button wins, then the command" requires.
	/// </remarks>
	private void OnCommandInputChanged(DependencyObject sender, DependencyProperty property)
	{
		//The command's keyboard accelerators are placed by the framework itself, in
		//CommandingHelpers.BindToKeyboardAcceleratorsIfUnset, which every ButtonBase runs when its
		//Command changes; nothing is needed here for them.
		ToolCommandSupport.SyncAccessKey(this, Command);
		UpdatePresentation();
	}

	/// <summary>Handles a change to one of the button's own presentation properties.</summary>
	/// <param name="sender">The button.</param>
	/// <param name="args">The change.</param>
	private static void OnPresentationPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((ToolButton)sender).UpdatePresentation();
}
