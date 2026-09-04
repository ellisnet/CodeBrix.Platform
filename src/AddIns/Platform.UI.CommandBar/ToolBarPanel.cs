using System.Collections.Generic;
using CodeBrix.Platform.UI.CommandBar.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The panel a <see cref="ToolBar"/> lays its items out in: one run of items along an axis, with a
/// gap between them, filling spacers taking what is left over, and separators snapped to the pixel
/// grid.
/// </summary>
/// <remarks>
/// <para>
/// The bar puts one of these in its template as <c>PART_ItemsHost</c> and a second one inside the
/// overflow flyout, turned across the bar's axis. It is public because a bar's template is
/// public - an application that re-templates <see cref="ToolBar"/> needs to be able to name the
/// part - and it is useful on its own wherever a row of tool bar items is wanted without a bar
/// around them.
/// </para>
/// <para>
/// The panel does not decide what overflows. The bar works that out before its template is
/// measured and moves the elements between the two panels, so the panel only ever lays out the
/// children it has been given.
/// </para>
/// </remarks>
public partial class ToolBarPanel : Panel
{
	/// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
	public static readonly DependencyProperty OrientationProperty =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(ToolBarPanel),
			new PropertyMetadata(Orientation.Horizontal, OnLayoutPropertyChanged));

	/// <summary>Identifies the <see cref="ItemSpacing"/> dependency property.</summary>
	public static readonly DependencyProperty ItemSpacingProperty =
		DependencyProperty.Register(
			nameof(ItemSpacing),
			typeof(double),
			typeof(ToolBarPanel),
			new PropertyMetadata(ToolBar.DefaultItemSpacing, OnLayoutPropertyChanged));

	/// <summary>Identifies the <see cref="Wrap"/> dependency property.</summary>
	public static readonly DependencyProperty WrapProperty =
		DependencyProperty.Register(
			nameof(Wrap),
			typeof(bool),
			typeof(ToolBarPanel),
			new PropertyMetadata(false, OnLayoutPropertyChanged));

	/// <summary>Initializes a new, empty panel.</summary>
	public ToolBarPanel()
	{
	}

	/// <summary>Gets or sets the axis the items run along.</summary>
	/// <value><see cref="Orientation.Horizontal"/> by default.</value>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>Gets or sets the gap between two adjacent items, in logical pixels.</summary>
	/// <value>Four logical pixels by default.</value>
	public double ItemSpacing
	{
		get => (double)GetValue(ItemSpacingProperty);
		set => SetValue(ItemSpacingProperty, value);
	}

	/// <summary>
	/// Gets or sets whether items that do not fit continue on a further line rather than running
	/// past the end of the panel.
	/// </summary>
	/// <value>False by default; a bar sets it when its overflow mode is
	/// <see cref="OverflowMode.Wrap"/>.</value>
	public bool Wrap
	{
		get => (bool)GetValue(WrapProperty);
		set => SetValue(WrapProperty, value);
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
		=> ToolBarLayout.Measure(Snapshot(), Orientation, ItemSpacing, availableSize, Wrap);

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize)
	{
		ToolBarLayout.Arrange(
			Snapshot(),
			Orientation,
			ItemSpacing,
			finalSize,
			ToolBarLayout.GetRasterizationScale(this),
			Wrap);

		return finalSize;
	}

	private IReadOnlyList<UIElement> Snapshot()
	{
		//Arranging reads DesiredSize, which the framework can only give for children still in the
		//collection; taking the list once keeps measure and arrange looking at the same children
		//even if a binding removes one in between.
		var children = Children;
		var snapshot = new UIElement[children.Count];
		for (var i = 0; i < snapshot.Length; i++)
		{
			snapshot[i] = children[i];
		}

		return snapshot;
	}

	private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((ToolBarPanel)d).InvalidateMeasure();
}
