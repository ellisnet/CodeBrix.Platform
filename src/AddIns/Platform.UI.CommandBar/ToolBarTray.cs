using System.Collections.Generic;
using CodeBrix.Platform.UI.CommandBar.Automation;
using CodeBrix.Platform.UI.CommandBar.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Hosts several <see cref="ToolBar"/>s side by side, wrapping to a further row when the width
/// runs out.
/// </summary>
/// <remarks>
/// <para>
/// A tray is the container an application puts at the top of a window when it has more than one
/// bar - a main bar and a music bar, say - and wants them on one line while there is room, and
/// stacked when there is not. With a single bar the tray adds nothing; use the bar on its own.
/// </para>
/// <para>
/// The tray is a panel, not a control: it has no template and no chrome of its own, and the bars
/// inside it carry the background and border. Its only settings are the orientation the bars are
/// laid along and the gap between them.
/// </para>
/// <para>
/// The tray is also the natural place to state the presentation settings for every bar under it -
/// <c>ToolBarProperties.IconSize</c>, <c>LabelMode</c>, <c>LabelPosition</c> and
/// <c>ShowToolTips</c> are inherited, so setting them here reaches every item in every bar.
/// </para>
/// </remarks>
public partial class ToolBarTray : Panel
{
	/// <summary>The gap a tray leaves between two bars when nothing sets one: 8 pixels.</summary>
	public const double DefaultToolBarSpacing = 8d;

	/// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
	public static readonly DependencyProperty OrientationProperty =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(ToolBarTray),
			new PropertyMetadata(Orientation.Horizontal, OnLayoutPropertyChanged));

	/// <summary>Identifies the <see cref="ToolBarSpacing"/> dependency property.</summary>
	public static readonly DependencyProperty ToolBarSpacingProperty =
		DependencyProperty.Register(
			nameof(ToolBarSpacing),
			typeof(double),
			typeof(ToolBarTray),
			new PropertyMetadata(DefaultToolBarSpacing, OnLayoutPropertyChanged));

	private readonly HashSet<ToolBar> _adoptedBars = new();

	/// <summary>Initializes a new, empty tray.</summary>
	public ToolBarTray()
	{
	}

	/// <summary>Gets or sets the axis the bars are laid along.</summary>
	/// <value>
	/// <see cref="Orientation.Horizontal"/> by default: bars side by side, wrapping downwards.
	/// </value>
	/// <remarks>
	/// The tray sets the matching orientation on every <see cref="ToolBar"/> it hosts that has not
	/// been given one of its own, so a vertical tray of vertical bars is one property, not two.
	/// </remarks>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Gets or sets the gap between two adjacent bars, and between two rows of them, in logical
	/// pixels.
	/// </summary>
	/// <value>Eight logical pixels by default.</value>
	public double ToolBarSpacing
	{
		get => (double)GetValue(ToolBarSpacingProperty);
		set => SetValue(ToolBarSpacingProperty, value);
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		SyncBarOrientation();

		return ToolBarLayout.Measure(Snapshot(), Orientation, ToolBarSpacing, availableSize, wrap: true);
	}

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize)
	{
		ToolBarLayout.Arrange(
			Snapshot(),
			Orientation,
			ToolBarSpacing,
			finalSize,
			ToolBarLayout.GetRasterizationScale(this),
			wrap: true);

		return finalSize;
	}

	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new ToolBarTrayAutomationPeer(this);

	private void SyncBarOrientation()
	{
		var children = Children;
		for (var i = 0; i < children.Count; i++)
		{
			//A bar that states its own orientation keeps it; the tray only fills in the blank.
			//Setting the value makes it local, so the bars the tray adopted are remembered - a
			//tray whose own orientation changes later still turns the bars it filled in.
			if (children[i] is ToolBar bar
				&& (_adoptedBars.Contains(bar)
					|| bar.ReadLocalValue(ToolBar.OrientationProperty) == DependencyProperty.UnsetValue))
			{
				_adoptedBars.Add(bar);
				bar.Orientation = Orientation;
			}
		}
	}

	private IReadOnlyList<UIElement> Snapshot()
	{
		var children = Children;
		var snapshot = new UIElement[children.Count];
		for (var i = 0; i < snapshot.Length; i++)
		{
			snapshot[i] = children[i];
		}

		return snapshot;
	}

	private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((ToolBarTray)d).InvalidateMeasure();
}
