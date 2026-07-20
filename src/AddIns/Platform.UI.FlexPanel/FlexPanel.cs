#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Flex = CodeBrix.Platform.UI.FlexPanel.Internal;

namespace CodeBrix.Platform.UI.FlexPanel;

//was previously: the panel behavior here is rewritten from src/Controls/src/Core/Layout/FlexLayout.cs,
//src/Core/src/Layouts/FlexLayoutManager.cs and src/Controls/src/Core/Layout/FlexExtensions.cs in
//dotnet/maui (MIT), re-expressed on the CodeBrix.Platform Panel model: DependencyProperty instead of
//BindableProperty, MeasureOverride/ArrangeOverride instead of ILayoutManager, and a flex item tree
//rebuilt on every layout pass instead of one persistently synced to child-collection changes
//(upstream re-initializes every item's properties on every pass anyway, so a persistent tree only
//saves allocations while costing a whole child-tracking surface).

/// <summary>
/// A CSS flexbox-style layout panel: children are arranged in optionally wrapping rows or columns,
/// with the familiar flexbox knobs. <see cref="Direction"/> picks the main axis,
/// <see cref="JustifyContent"/> distributes free space along it, <see cref="AlignItems"/> aligns
/// children across it, <see cref="Wrap"/> allows multiple lines, and <see cref="AlignContent"/>
/// distributes those lines. Per-child behavior is set with the attached properties
/// <c>FlexPanel.Grow</c>, <c>FlexPanel.Shrink</c>, <c>FlexPanel.Basis</c>, <c>FlexPanel.Order</c>
/// and <c>FlexPanel.AlignSelf</c>.
/// </summary>
/// <remarks>
/// <para>
/// The layout engine is a managed port of the .NET MAUI FlexLayout engine, so layout results match
/// what the same tree of sizes produces there. Child <c>Margin</c> values participate in the flex
/// algorithm exactly as CSS margins do: they occupy main-axis space between siblings and offset
/// cross-axis alignment.
/// </para>
/// <para>
/// In XAML, map the namespace with
/// <c>xmlns:flex="using:CodeBrix.Platform.UI.FlexPanel"</c> and use
/// <c>&lt;flex:FlexPanel Direction="Row" Wrap="Wrap"&gt;</c>.
/// </para>
/// </remarks>
[Bindable]
public partial class FlexPanel : Panel
{
	#region Panel-level dependency properties

	/// <summary>Identifies the <see cref="Direction"/> dependency property.</summary>
	public static readonly DependencyProperty DirectionProperty =
		DependencyProperty.Register(nameof(Direction), typeof(FlexDirection), typeof(FlexPanel),
			new PropertyMetadata(FlexDirection.Row, OnPanelPropertyChanged));

	/// <summary>Identifies the <see cref="JustifyContent"/> dependency property.</summary>
	public static readonly DependencyProperty JustifyContentProperty =
		DependencyProperty.Register(nameof(JustifyContent), typeof(FlexJustify), typeof(FlexPanel),
			new PropertyMetadata(FlexJustify.Start, OnPanelPropertyChanged));

	/// <summary>Identifies the <see cref="AlignContent"/> dependency property.</summary>
	public static readonly DependencyProperty AlignContentProperty =
		DependencyProperty.Register(nameof(AlignContent), typeof(FlexAlignContent), typeof(FlexPanel),
			new PropertyMetadata(FlexAlignContent.Stretch, OnPanelPropertyChanged));

	/// <summary>Identifies the <see cref="AlignItems"/> dependency property.</summary>
	public static readonly DependencyProperty AlignItemsProperty =
		DependencyProperty.Register(nameof(AlignItems), typeof(FlexAlignItems), typeof(FlexPanel),
			new PropertyMetadata(FlexAlignItems.Stretch, OnPanelPropertyChanged));

	/// <summary>Identifies the <see cref="Wrap"/> dependency property.</summary>
	public static readonly DependencyProperty WrapProperty =
		DependencyProperty.Register(nameof(Wrap), typeof(FlexWrap), typeof(FlexPanel),
			new PropertyMetadata(FlexWrap.NoWrap, OnPanelPropertyChanged));

	/// <summary>Identifies the <see cref="Position"/> dependency property.</summary>
	public static readonly DependencyProperty PositionProperty =
		DependencyProperty.Register(nameof(Position), typeof(FlexPosition), typeof(FlexPanel),
			new PropertyMetadata(FlexPosition.Relative, OnPanelPropertyChanged));

	/// <summary>Identifies the <see cref="Padding"/> dependency property.</summary>
	public static readonly DependencyProperty PaddingProperty =
		DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(FlexPanel),
			new PropertyMetadata(default(Thickness), OnPanelPropertyChanged));

	/// <summary>
	/// Gets or sets the direction and main axis along which children are stacked. The default is
	/// <see cref="FlexDirection.Row"/>.
	/// </summary>
	public FlexDirection Direction
	{
		get => (FlexDirection)GetValue(DirectionProperty);
		set => SetValue(DirectionProperty, value);
	}

	/// <summary>
	/// Gets or sets how free main-axis space is distributed between and around children. The
	/// default is <see cref="FlexJustify.Start"/>.
	/// </summary>
	public FlexJustify JustifyContent
	{
		get => (FlexJustify)GetValue(JustifyContentProperty);
		set => SetValue(JustifyContentProperty, value);
	}

	/// <summary>
	/// Gets or sets how lines are distributed on the cross axis when the panel wraps onto multiple
	/// lines. Ignored while <see cref="Wrap"/> is <see cref="FlexWrap.NoWrap"/>. The default is
	/// <see cref="FlexAlignContent.Stretch"/>.
	/// </summary>
	public FlexAlignContent AlignContent
	{
		get => (FlexAlignContent)GetValue(AlignContentProperty);
		set => SetValue(AlignContentProperty, value);
	}

	/// <summary>
	/// Gets or sets how children are aligned on the cross axis of their line. Individual children
	/// can override this with the <c>FlexPanel.AlignSelf</c> attached property. The default is
	/// <see cref="FlexAlignItems.Stretch"/>.
	/// </summary>
	public FlexAlignItems AlignItems
	{
		get => (FlexAlignItems)GetValue(AlignItemsProperty);
		set => SetValue(AlignItemsProperty, value);
	}

	/// <summary>
	/// Gets or sets whether children are kept on a single line or wrap onto multiple lines. The
	/// default is <see cref="FlexWrap.NoWrap"/>.
	/// </summary>
	public FlexWrap Wrap
	{
		get => (FlexWrap)GetValue(WrapProperty);
		set => SetValue(WrapProperty, value);
	}

	/// <summary>
	/// Gets or sets whether children are positioned by flexbox rules or fixed coordinates. Present
	/// for API parity with the .NET MAUI FlexLayout this panel is ported from; like the original,
	/// the current engine always positions children with the flexbox rules, so
	/// <see cref="FlexPosition.Absolute"/> has no effect on child arrangement.
	/// </summary>
	public FlexPosition Position
	{
		get => (FlexPosition)GetValue(PositionProperty);
		set => SetValue(PositionProperty, value);
	}

	/// <summary>
	/// Gets or sets the padding between the panel's edges and the area used to lay out children.
	/// </summary>
	public Thickness Padding
	{
		get => (Thickness)GetValue(PaddingProperty);
		set => SetValue(PaddingProperty, value);
	}

	#endregion

	#region Attached dependency properties

	/// <summary>
	/// Identifies the <c>FlexPanel.Order</c> attached property: children are arranged by ascending
	/// order value (insertion order breaks ties). The default is 0.
	/// </summary>
	public static readonly DependencyProperty OrderProperty =
		DependencyProperty.RegisterAttached("Order", typeof(int), typeof(FlexPanel),
			new PropertyMetadata(0, OnChildPropertyChanged));

	/// <summary>
	/// Identifies the <c>FlexPanel.Grow</c> attached property: the proportion of free main-axis
	/// space this child takes. The default is 0 (the child does not grow). Must not be negative.
	/// </summary>
	public static readonly DependencyProperty GrowProperty =
		DependencyProperty.RegisterAttached("Grow", typeof(float), typeof(FlexPanel),
			new PropertyMetadata(0f, OnChildGrowShrinkPropertyChanged));

	/// <summary>
	/// Identifies the <c>FlexPanel.Shrink</c> attached property: how much this child shrinks,
	/// relative to its siblings, when the children overflow the main axis. The default is 1 (all
	/// children shrink equally). Must not be negative.
	/// </summary>
	public static readonly DependencyProperty ShrinkProperty =
		DependencyProperty.RegisterAttached("Shrink", typeof(float), typeof(FlexPanel),
			new PropertyMetadata(1f, OnChildGrowShrinkPropertyChanged));

	/// <summary>
	/// Identifies the <c>FlexPanel.AlignSelf</c> attached property: a per-child override of the
	/// panel's <see cref="AlignItems"/>. The default is <see cref="FlexAlignSelf.Auto"/>.
	/// </summary>
	public static readonly DependencyProperty AlignSelfProperty =
		DependencyProperty.RegisterAttached("AlignSelf", typeof(FlexAlignSelf), typeof(FlexPanel),
			new PropertyMetadata(FlexAlignSelf.Auto, OnChildPropertyChanged));

	/// <summary>
	/// Identifies the <c>FlexPanel.Basis</c> attached property: the child's initial main-axis size,
	/// as a <see cref="FlexBasis"/> (auto, absolute, or a percentage of the panel's main axis). The
	/// default is <see cref="FlexBasis.Auto"/>.
	/// </summary>
	public static readonly DependencyProperty BasisProperty =
		DependencyProperty.RegisterAttached("Basis", typeof(FlexBasis), typeof(FlexPanel),
			new PropertyMetadata(FlexBasis.Auto, OnChildPropertyChanged));

	/// <summary>Gets the <c>FlexPanel.Order</c> attached property value for an element.</summary>
	/// <param name="element">The element to read the value from.</param>
	/// <returns>The element's order among its siblings.</returns>
	public static int GetOrder(DependencyObject element) => (int)element.GetValue(OrderProperty);

	/// <summary>Sets the <c>FlexPanel.Order</c> attached property value for an element.</summary>
	/// <param name="element">The element to set the value on.</param>
	/// <param name="value">The order value; children are arranged by ascending value.</param>
	public static void SetOrder(DependencyObject element, int value) => element.SetValue(OrderProperty, value);

	/// <summary>Gets the <c>FlexPanel.Grow</c> attached property value for an element.</summary>
	/// <param name="element">The element to read the value from.</param>
	/// <returns>The element's grow factor.</returns>
	public static float GetGrow(DependencyObject element) => (float)element.GetValue(GrowProperty);

	/// <summary>Sets the <c>FlexPanel.Grow</c> attached property value for an element.</summary>
	/// <param name="element">The element to set the value on.</param>
	/// <param name="value">The grow factor; must not be negative.</param>
	public static void SetGrow(DependencyObject element, float value) => element.SetValue(GrowProperty, value);

	/// <summary>Gets the <c>FlexPanel.Shrink</c> attached property value for an element.</summary>
	/// <param name="element">The element to read the value from.</param>
	/// <returns>The element's shrink factor.</returns>
	public static float GetShrink(DependencyObject element) => (float)element.GetValue(ShrinkProperty);

	/// <summary>Sets the <c>FlexPanel.Shrink</c> attached property value for an element.</summary>
	/// <param name="element">The element to set the value on.</param>
	/// <param name="value">The shrink factor; must not be negative.</param>
	public static void SetShrink(DependencyObject element, float value) => element.SetValue(ShrinkProperty, value);

	/// <summary>Gets the <c>FlexPanel.AlignSelf</c> attached property value for an element.</summary>
	/// <param name="element">The element to read the value from.</param>
	/// <returns>The element's cross-axis alignment override.</returns>
	public static FlexAlignSelf GetAlignSelf(DependencyObject element) => (FlexAlignSelf)element.GetValue(AlignSelfProperty);

	/// <summary>Sets the <c>FlexPanel.AlignSelf</c> attached property value for an element.</summary>
	/// <param name="element">The element to set the value on.</param>
	/// <param name="value">The cross-axis alignment override.</param>
	public static void SetAlignSelf(DependencyObject element, FlexAlignSelf value) => element.SetValue(AlignSelfProperty, value);

	/// <summary>Gets the <c>FlexPanel.Basis</c> attached property value for an element.</summary>
	/// <param name="element">The element to read the value from.</param>
	/// <returns>The element's initial main-axis size.</returns>
	public static FlexBasis GetBasis(DependencyObject element) => (FlexBasis)element.GetValue(BasisProperty);

	/// <summary>Sets the <c>FlexPanel.Basis</c> attached property value for an element.</summary>
	/// <param name="element">The element to set the value on.</param>
	/// <param name="value">The initial main-axis size.</param>
	public static void SetBasis(DependencyObject element, FlexBasis value) => element.SetValue(BasisProperty, value);

	#endregion

	#region Property-changed handlers

	private static void OnPanelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((FlexPanel)d).InvalidateMeasure();

	private static void OnChildGrowShrinkPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if ((float)e.NewValue < 0f)
			throw new ArgumentException("FlexPanel.Grow and FlexPanel.Shrink must not be negative.");
		OnChildPropertyChanged(d, e);
	}

	private static void OnChildPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		// An attached value changed on a child: re-run the owning panel's layout. A child's own
		// measure invalidation is not enough - Order or Grow changes rearrange the panel without
		// changing the child's desired size.
		if (d is UIElement element && VisualTreeHelper.GetParent(element) is FlexPanel panel)
			panel.InvalidateMeasure();
	}

	#endregion

	#region Measure and arrange

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		var padding = Padding;
		var availableWidth = Math.Max(0, availableSize.Width - padding.Left - padding.Right);
		var availableHeight = Math.Max(0, availableSize.Height - padding.Top - padding.Bottom);

		// Shrink and cross-axis Stretch need a fixed area to make sense. When a dimension is
		// unconstrained, build the items with Shrink=0 and AlignSelf=Start instead: the panel then
		// measures to its natural size (the upstream FlexLayout "measure hack", expressed here as
		// build-time values because the item tree is rebuilt on every pass anyway).
		var naturalSizeMeasure = double.IsInfinity(availableWidth) || double.IsInfinity(availableHeight);

		var pairs = new List<(UIElement Child, Flex.Item Item)>(Children.Count);
		var root = BuildItemTree(availableWidth, availableHeight, naturalSizeMeasure, pairs);
		root.Layout(inMeasureMode: true);

		double measuredWidth = 0;
		double measuredHeight = 0;
		foreach (var (child, item) in pairs)
		{
			if (!item.IsVisible)
			{
				// Keep the measure contract for collapsed children; their desired size is zero.
				child.Measure(availableSize);
				continue;
			}

			var margin = GetMargin(child);
			// The engine frame starts past the leading margin; adding the trailing margin makes
			// the measured extent cover the child's full margin box.
			measuredWidth = Math.Max(measuredWidth, NanToZero(item.Frame[0] + item.Frame[2] + margin.Right));
			measuredHeight = Math.Max(measuredHeight, NanToZero(item.Frame[1] + item.Frame[3] + margin.Bottom));
		}

		return new Size(
			measuredWidth + padding.Left + padding.Right,
			measuredHeight + padding.Top + padding.Bottom);
	}

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize)
	{
		var padding = Padding;
		var availableWidth = Math.Max(0, finalSize.Width - padding.Left - padding.Right);
		var availableHeight = Math.Max(0, finalSize.Height - padding.Top - padding.Bottom);

		var pairs = new List<(UIElement Child, Flex.Item Item)>(Children.Count);
		var root = BuildItemTree(availableWidth, availableHeight, naturalSizeMeasure: false, pairs);
		root.Layout(inMeasureMode: false);

		foreach (var (child, item) in pairs)
		{
			if (!item.IsVisible)
			{
				child.Arrange(default(Rect));
				continue;
			}

			var margin = GetMargin(child);
			// The engine computes the margin-exclusive content frame; the arrange rect is the
			// child's layout slot, which includes the margin space the framework peels back off,
			// landing the child exactly on the engine's frame.
			var x = NanToZero(padding.Left + item.Frame[0] - margin.Left);
			var y = NanToZero(padding.Top + item.Frame[1] - margin.Top);
			var width = Math.Max(0, NanToZero(item.Frame[2] + margin.Left + margin.Right));
			var height = Math.Max(0, NanToZero(item.Frame[3] + margin.Top + margin.Bottom));
			child.Arrange(new Rect(x, y, width, height));
		}

		return finalSize;
	}

	private Flex.Item BuildItemTree(double width, double height, bool naturalSizeMeasure, List<(UIElement Child, Flex.Item Item)> pairs)
	{
		var root = new Flex.Item
		{
			Direction = (Flex.Direction)Direction,
			JustifyContent = (Flex.Justify)JustifyContent,
			AlignContent = (Flex.AlignContent)AlignContent,
			AlignItems = (Flex.AlignItems)AlignItems,
			Wrap = (Flex.Wrap)Wrap,
			Position = (Flex.Position)Position,
			// The engine needs concrete root dimensions; an unconstrained dimension is expressed
			// as 0, which the engine reads as "measuring unconstrained in that direction".
			Width = double.IsInfinity(width) ? 0f : (float)width,
			Height = double.IsInfinity(height) ? 0f : (float)height,
		};

		foreach (var child in Children)
		{
			var item = new Flex.Item
			{
				Order = GetOrder(child),
				Grow = GetGrow(child),
				Shrink = naturalSizeMeasure ? 0f : GetShrink(child),
				AlignSelf = naturalSizeMeasure ? Flex.AlignSelf.Start : (Flex.AlignSelf)GetAlignSelf(child),
				Basis = ToFlexBasis(GetBasis(child)),
				IsVisible = child.Visibility != Visibility.Collapsed,
			};

			var margin = GetMargin(child);
			item.MarginLeft = (float)margin.Left;
			item.MarginTop = (float)margin.Top;
			item.MarginRight = (float)margin.Right;
			item.MarginBottom = (float)margin.Bottom;

			if (child is FrameworkElement fe)
			{
				item.Width = double.IsNaN(fe.Width) ? float.NaN : (float)fe.Width;
				item.Height = double.IsNaN(fe.Height) ? float.NaN : (float)fe.Height;
			}

			SetSelfSizing(child, item);

			// Order is already set, so Add sees it and turns on the engine's ordered enumeration.
			root.Add(item);
			pairs.Add((child, item));
		}

		return root;
	}

	private static void SetSelfSizing(UIElement child, Flex.Item item)
	{
		item.SelfSizing = (Flex.Item it, ref float w, ref float h, bool inMeasureMode) =>
		{
			var margin = GetMargin(child);

			if (inMeasureMode)
				child.Measure(GetConstraints(it));
			// In the arrange pass, never measure - reuse the measure pass's DesiredSize.
			var desired = child.DesiredSize;

			// DesiredSize includes the child's margin; the engine works with margin-exclusive
			// sizes (the margins are fed to the engine separately).
			var desiredWidth = (float)Math.Max(0, desired.Width - margin.Left - margin.Right);
			var desiredHeight = (float)Math.Max(0, desired.Height - margin.Top - margin.Bottom);

			// When the child has an explicit Width/Height the item already carries it; returning
			// NaN in the arrange pass preserves that value instead of overwriting it with a
			// potentially stale desired size (matches the upstream FlexLayout rule).
			w = (!inMeasureMode && !float.IsNaN(it.Width)) ? float.NaN : desiredWidth;
			h = (!inMeasureMode && !float.IsNaN(it.Height)) ? float.NaN : desiredHeight;
		};
	}

	//was previously: FlexExtensions.GetConstraints in dotnet/maui, plus the zero-to-infinity swap
	//from FlexLayout's SelfSizing callback (a zero root dimension means "measuring unconstrained").
	private static Size GetConstraints(Flex.Item item)
	{
		var widthConstraint = -1d;
		var heightConstraint = -1d;
		var parent = item.Parent;
		do
		{
			if (parent == null)
				break;
			if (widthConstraint < 0 && !float.IsNaN(parent.Width))
				widthConstraint = parent.Width;
			if (heightConstraint < 0 && !float.IsNaN(parent.Height))
				heightConstraint = parent.Height;
			parent = parent.Parent;
		} while (widthConstraint < 0 || heightConstraint < 0);

		return new Size(
			widthConstraint <= 0 ? double.PositiveInfinity : widthConstraint,
			heightConstraint <= 0 ? double.PositiveInfinity : heightConstraint);
	}

	private static Flex.Basis ToFlexBasis(FlexBasis basis)
		=> basis.IsAuto ? Flex.Basis.Auto : new Flex.Basis(basis.Length, basis.IsRelative);

	private static Thickness GetMargin(UIElement child)
		=> child is FrameworkElement fe ? fe.Margin : default;

	private static double NanToZero(double value)
		=> double.IsNaN(value) ? 0 : value;

	#endregion
}
