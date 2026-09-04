using System;
using CodeBrix.Platform.UI.CommandBar.Automation;
using CodeBrix.Platform.UI.CommandBar.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The divider between two runs of items: a vertical line in a horizontal bar, a horizontal line
/// in a vertical one.
/// </summary>
/// <remarks>
/// <para>
/// The line is one DEVICE pixel wide whatever the display scale, and it is snapped to the pixel
/// grid, so it reads as a hairline rather than as a soft two-pixel smear at a fractional scale.
/// Its colour comes from the add-in's theme resources, so it follows a live theme change.
/// </para>
/// <para>
/// <see cref="Thickness"/> is counted in DEVICE pixels, not logical ones: the separator asks for
/// <c>Thickness / RasterizationScale</c> logical pixels, which is exactly <see cref="Thickness"/>
/// device pixels once the display scale is applied. A bar sets <see cref="Orientation"/> on every
/// separator it hosts, so a separator written in XAML never has to state which way it runs.
/// </para>
/// </remarks>
public partial class ToolBarSeparator : Control
{
	/// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
	public static readonly DependencyProperty OrientationProperty =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(ToolBarSeparator),
			new PropertyMetadata(Orientation.Vertical, OnLayoutPropertyChanged));

	/// <summary>Identifies the <see cref="Thickness"/> dependency property.</summary>
	public static readonly DependencyProperty ThicknessProperty =
		DependencyProperty.Register(
			nameof(Thickness),
			typeof(double),
			typeof(ToolBarSeparator),
			new PropertyMetadata(1d, OnLayoutPropertyChanged));

	/// <summary>Initializes a new separator.</summary>
	public ToolBarSeparator()
	{
		DefaultStyleKey = typeof(ToolBarSeparator);
	}

	/// <summary>
	/// Gets or sets which way the line runs: <see cref="Orientation.Vertical"/> (the default) for a
	/// horizontal bar, <see cref="Orientation.Horizontal"/> for a vertical one.
	/// </summary>
	/// <value>The direction of the drawn line.</value>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>Gets or sets the line's thickness in DEVICE pixels.</summary>
	/// <value>The number of device pixels the line covers; 1 by default.</value>
	public double Thickness
	{
		get => (double)GetValue(ThicknessProperty);
		set => SetValue(ThicknessProperty, value);
	}

	/// <summary>
	/// Gets the line's thickness in logical pixels at the display scale this separator is drawn at.
	/// </summary>
	/// <value>
	/// <see cref="Thickness"/> divided by the rasterization scale, so the drawn line covers exactly
	/// <see cref="Thickness"/> device pixels.
	/// </value>
	public double LogicalThickness
	{
		get
		{
			var scale = ToolBarLayout.GetRasterizationScale(this);
			return Math.Max(0, Thickness) / scale;
		}
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		//The template child is measured so it can size itself, but the separator's own desired
		//extent across the line is the hairline width - never what the child asked for.
		base.MeasureOverride(availableSize);

		var line = LogicalThickness;

		return Orientation == Orientation.Vertical
			? new Size(line, 0)
			: new Size(0, line);
	}

	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new ToolBarSeparatorAutomationPeer(this);

	private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((ToolBarSeparator)d).InvalidateMeasure();
}
