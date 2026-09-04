using System.Collections.Generic;
using CodeBrix.Platform.UI.CommandBar.Automation;
using CodeBrix.Platform.UI.CommandBar.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// A run of tool bar items that belong together, with its own spacing and an automatic separator
/// between it and the next group.
/// </summary>
/// <remarks>
/// <para>
/// Grouping is also possible without this container, by placing <see cref="ToolBarSeparator"/> and
/// <see cref="ToolBarSpacer"/> items in the bar by hand. The container earns its place when the
/// groups are data-driven, or when the separators should appear and disappear with the items
/// rather than being written out one by one.
/// </para>
/// <para>
/// A group is one item as far as the bar is concerned: it moves into the overflow flyout whole,
/// never half. Its <see cref="Orientation"/> is set by the bar that hosts it, so a group written
/// in XAML follows the bar it is put in.
/// </para>
/// </remarks>
public partial class ToolBarGroup : Panel
{
	/// <summary>The gap a group leaves between its own items when nothing sets one: 4 pixels.</summary>
	public const double DefaultSpacing = 4d;

	/// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
	public static readonly DependencyProperty OrientationProperty =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(ToolBarGroup),
			new PropertyMetadata(Orientation.Horizontal, OnLayoutPropertyChanged));

	/// <summary>Identifies the <see cref="Spacing"/> dependency property.</summary>
	public static readonly DependencyProperty SpacingProperty =
		DependencyProperty.Register(
			nameof(Spacing),
			typeof(double),
			typeof(ToolBarGroup),
			new PropertyMetadata(DefaultSpacing, OnLayoutPropertyChanged));

	/// <summary>Initializes a new, empty group.</summary>
	public ToolBarGroup()
	{
	}

	/// <summary>Gets or sets the axis the group's items run along.</summary>
	/// <value><see cref="Orientation.Horizontal"/> by default; a bar sets it to match itself.</value>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>Gets or sets the gap between two adjacent items in the group.</summary>
	/// <value>Four logical pixels by default - tighter than the gap between groups.</value>
	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
		=> ToolBarLayout.Measure(Snapshot(), Orientation, Spacing, availableSize);

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize)
	{
		ToolBarLayout.Arrange(
			Snapshot(),
			Orientation,
			Spacing,
			finalSize,
			ToolBarLayout.GetRasterizationScale(this));

		return finalSize;
	}

	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new ToolBarGroupAutomationPeer(this);

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
		=> ((ToolBarGroup)d).InvalidateMeasure();
}
