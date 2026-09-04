using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Empty space in a bar: either a fixed width, or all the space that is left.
/// </summary>
/// <remarks>
/// <para>
/// A filling spacer is how the trailing items of a bar are pushed to the far end - the usual
/// arrangement for a zoom control or a page indicator that belongs at the right of a tool bar
/// while the commands stay at the left. Set <see cref="Fill"/> to true for that; set the inherited
/// <see cref="FrameworkElement.Width"/> (or <see cref="FrameworkElement.Height"/> in a vertical
/// bar) for a fixed gap.
/// </para>
/// <para>
/// Several filling spacers in one bar share the space left over equally, which is how a bar is
/// split into left, centre and right runs with two of them.
/// </para>
/// <para>
/// A filling spacer asks for nothing of its own, so a bar that is measured against an unbounded
/// width - inside a horizontally scrolling parent, say - has no space left over to give it and the
/// spacer collapses to nothing. That is the same answer a star-sized grid column gives in the same
/// place, and it is why a bar that must fill needs a parent that constrains it.
/// </para>
/// </remarks>
public partial class ToolBarSpacer : FrameworkElement
{
	/// <summary>Identifies the <see cref="Fill"/> dependency property.</summary>
	public static readonly DependencyProperty FillProperty =
		DependencyProperty.Register(
			nameof(Fill),
			typeof(bool),
			typeof(ToolBarSpacer),
			new PropertyMetadata(false, OnFillChanged));

	/// <summary>Initializes a new spacer.</summary>
	public ToolBarSpacer()
	{
	}

	/// <summary>
	/// Gets or sets whether the spacer takes all the space the bar has left over, rather than a
	/// fixed size.
	/// </summary>
	/// <value>True to fill; false (the default) for a fixed gap.</value>
	public bool Fill
	{
		get => (bool)GetValue(FillProperty);
		set => SetValue(FillProperty, value);
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		//A spacer has no content, so its desired size is exactly what was asked for: an explicit
		//Width/Height (applied by the framework before this is called, arriving as availableSize)
		//or nothing. A filling spacer asks for nothing here and is grown during arrange, so the
		//other items are measured against the real space first.
		var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
		var height = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height;

		return new Size(
			double.IsNaN(Width) ? 0 : width,
			double.IsNaN(Height) ? 0 : height);
	}

	private static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((ToolBarSpacer)d).InvalidateMeasure();
}
