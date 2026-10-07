#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// The panel itself (not the engine): a child whose main-axis size the flex algorithm changes (shrinks or grows)
/// is measured at that final size, so it lays out at the size it is arranged at - a child measured only at the
/// panel's full width would keep that larger layout and be clipped.
/// </summary>
/// <remarks>
/// These tests build XAML elements, so they run one at a time (see the collection below) on the host-free
/// dispatcher bootstrap in DispatcherInitializer.cs. The children are plain elements - no text, no templates.
/// </remarks>
[Collection(nameof(FlexPanelLayoutTests))]
[CollectionDefinition(nameof(FlexPanelLayoutTests), DisableParallelization = true)]
public class FlexPanelLayoutTests
{
	/// <summary>
	/// Behaves like a Grid holding a long text in a star column: it wants <see cref="Natural"/> wide, but takes
	/// whatever narrower width it is offered.
	/// </summary>
	private sealed class ElasticElement : FrameworkElement
	{
		public double Natural { get; init; } = 1000;
		public double ContentHeight { get; init; } = 20;
		public int MeasureCount { get; private set; }
		public double LastMeasureWidth { get; private set; }
		public double LastArrangeWidth { get; private set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureCount++;
			LastMeasureWidth = availableSize.Width;
			return new Size(System.Math.Min(availableSize.Width, Natural), ContentHeight);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			LastArrangeWidth = finalSize.Width;
			return finalSize;
		}
	}

	[Fact]
	public void a_shrunk_child_is_measured_and_laid_out_at_its_final_width()
	{
		//Arrange - a fixed 100-wide sibling that does not shrink, like a "Select..." button
		var panel = new FlexPanel();
		var elastic = new ElasticElement();
		var button = new Border { Width = 100, Height = 20 };
		FlexPanel.SetShrink(button, 0);
		panel.Children.Add(elastic);
		panel.Children.Add(button);

		//Act
		LayOut(panel, 300, 40);

		//Assert
		Assert.Equal(200, elastic.LastMeasureWidth, 3);
		Assert.Equal(200, elastic.DesiredSize.Width, 3);
		Assert.Equal(200, elastic.LastArrangeWidth, 3);
		Assert.Equal(200, elastic.ActualWidth, 3);
		Assert.Equal(new Rect(200, 0, 100, 20), Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(button));
	}

	[Fact]
	public void children_that_shrink_together_are_each_laid_out_at_their_share()
	{
		//Arrange - both shrink (the default); the elastic child's basis is the full 300 it was offered
		var panel = new FlexPanel();
		var elastic = new ElasticElement();
		var button = new Border { Width = 100, Height = 20 };
		panel.Children.Add(elastic);
		panel.Children.Add(button);

		//Act
		LayOut(panel, 300, 40);

		//Assert - 100 too much; the engine takes it from the children by their shrink factors (1 : 1),
		//so each gives up 50
		Assert.Equal(250, elastic.ActualWidth, 3);
		Assert.Equal(250, elastic.DesiredSize.Width, 3);
		Assert.Equal(250, Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(button).X, 3);
	}

	[Fact]
	public void a_shrunk_child_with_margins_is_measured_inside_its_margins()
	{
		//Arrange
		var panel = new FlexPanel();
		var elastic = new ElasticElement { Margin = new Thickness(10, 0, 10, 0) };
		var button = new Border { Width = 100, Height = 20 };
		FlexPanel.SetShrink(button, 0);
		panel.Children.Add(elastic);
		panel.Children.Add(button);

		//Act
		LayOut(panel, 300, 40);

		//Assert - 300 less the button and the 20 of margins
		Assert.Equal(180, elastic.LastMeasureWidth, 3);
		Assert.Equal(180, elastic.ActualWidth, 3);
		Assert.Equal(200, Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(button).X, 3);
	}

	[Fact]
	public void a_grown_child_is_measured_at_its_final_width()
	{
		//Arrange - a narrow elastic child that grows into the free space
		var panel = new FlexPanel();
		var elastic = new ElasticElement { Natural = 50 };
		FlexPanel.SetGrow(elastic, 1);
		panel.Children.Add(elastic);
		panel.Children.Add(new Border { Width = 100, Height = 20 });

		//Act
		LayOut(panel, 300, 40);

		//Assert
		Assert.Equal(200, elastic.LastMeasureWidth, 3);
		Assert.Equal(200, elastic.ActualWidth, 3);
	}

	[Fact]
	public void a_child_that_keeps_its_size_is_measured_once_per_pass()
	{
		//Arrange
		var panel = new FlexPanel();
		var elastic = new ElasticElement { Natural = 120 };
		panel.Children.Add(elastic);
		panel.Children.Add(new Border { Width = 100, Height = 20 });

		//Act
		LayOut(panel, 300, 40);

		//Assert
		Assert.Equal(1, elastic.MeasureCount);
		Assert.Equal(120, elastic.ActualWidth, 3);
	}

	[Fact]
	public void the_layout_settles_without_measuring_again()
	{
		//Arrange
		var panel = new FlexPanel();
		var elastic = new ElasticElement();
		var button = new Border { Width = 100, Height = 20 };
		FlexPanel.SetShrink(button, 0);
		panel.Children.Add(elastic);
		panel.Children.Add(button);
		LayOut(panel, 300, 40);
		var countAfterFirstPass = elastic.MeasureCount;

		//Act - the same constraints again: nothing is invalid, so nothing is measured
		LayOut(panel, 300, 40);

		//Assert
		Assert.Equal(2, countAfterFirstPass);
		Assert.Equal(countAfterFirstPass, elastic.MeasureCount);
		Assert.Equal(200, elastic.ActualWidth, 3);
	}

	private static void LayOut(FrameworkElement element, double width, double height)
	{
		element.Measure(new Size(width, height));
		element.Arrange(new Rect(0, 0, width, height));
	}
}
