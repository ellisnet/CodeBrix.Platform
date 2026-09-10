//The ported code keeps the original's nullable-oblivious style (a settable Func field on the stub
//child, and `default` where a Size is meant), so the file opts out rather than being rewritten
//around annotations it was never written for.
#nullable disable

using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SilverAssertions;
using SilverAssertions.Execution;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.Headless.Tests;

/// <summary>
/// <see cref="Grid"/> layout: what a grid asks for, and where it puts its children, for rows and
/// columns of every kind - absolute, star and auto - with spans, alignment, margins and padding.
/// </summary>
/// <remarks>
/// <para>
/// Ported from src/Platform.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_GridLayouting.cs.
/// This is the layout slice: every test here drives Measure and Arrange by hand against fixed
/// sizes and asserts on DesiredSize and on the sizes handed to a stub child's overrides. No text is
/// measured, so nothing depends on which fonts the machine has, and no Window, XamlRoot or
/// compositor is involved - which is what lets it read the same on Linux, Windows and macOS.
/// </para>
/// <para>
/// NOT PORTED: the tests the original marks [Ignore] (they are turned off upstream and porting them
/// would only move a disabled test), and the handful that await a real dispatcher turn to see a
/// RowDefinitions/ColumnDefinitions change take effect - inline dispatch cannot stand in for a
/// layout pass. Both groups stay in Platform.UI.RuntimeTests with their original reasons intact.
/// </para>
/// </remarks>
public partial class GridLayoutingTests
{
	private partial class View : FrameworkElement
	{
		public Size SizePassedToMeasureOverride { get; private set; }
		public Size SizePassedToArrangeOverride { get; private set; }
		public Size? RequestedDesiredSize { get; set; }
		internal Func<Size, Size> DesiredSizeSelector { get; set; }
		internal int MeasureCallCount { get; private set; }
		internal int ArrangeCallCount { get; private set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureCallCount++;
			SizePassedToMeasureOverride = availableSize;
			if (DesiredSizeSelector != null)
			{
				return DesiredSizeSelector(availableSize);
			}
			else if (RequestedDesiredSize != null)
			{
				return RequestedDesiredSize.Value;
			}

			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			ArrangeCallCount++;
			SizePassedToArrangeOverride = finalSize;
			return base.ArrangeOverride(finalSize);
		}
	}

	[Fact]
	public void When_Empty_And_MeasuredEmpty()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.Measure(default);
		var size = SUT.DesiredSize;
		SUT.Arrange(default);

		size.Should().Be(default(Size));
		SUT.Children.Should().BeEmpty();
	}

	[Fact]
	public void When_Empty_And_Measured_Non_Empty()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid { Name = "test" };

		SUT.Measure(new Size(10, 10));
		var size = SUT.DesiredSize;
		SUT.Arrange(default);

		size.Should().Be(default(Size));
		SUT.Children.Should().BeEmpty();
	}

	[Fact]
	public void When_Column_Out_Of_Range()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid();

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });

		var c1 = new View { RequestedDesiredSize = new Size(100, 100) };
		SUT.Children.Add(c1);

		var c2 = new View { RequestedDesiredSize = new Size(100, 100) };
		Grid.SetColumn(c2, 3);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(10, 10));
		SUT.DesiredSize.Should().Be(new Size(10, 10));
		//SUT.UnclippedDesiredSize.Should().Be(new Size(200, 105));

		SUT.Arrange(new Rect(0, 0, 10, 10));
	}

	[Fact]
	public void When_RowSpan_Out_Of_Range()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid();

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { RequestedDesiredSize = new Size(100, 100) };
		SUT.Children.Add(c1);

		var c2 = new View { RequestedDesiredSize = new Size(100, 100) };
		Grid.SetRow(c2, 1);
		Grid.SetColumn(c2, 0);
		Grid.SetRowSpan(c2, 3);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(100, 1000));
		SUT.DesiredSize.Should().Be(new Size(100, 200));
		//SUT.UnclippedDesiredSize.Should().Be(new Size(100, 200));

		SUT.Arrange(new Rect(0, 0, 100, 1000));
	}

	[Fact]
	public void When_ColumnSpan_Out_Of_Range()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid();

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { RequestedDesiredSize = new Size(100, 100) };
		SUT.Children.Add(c1);

		var c2 = new View { RequestedDesiredSize = new Size(100, 100) };
		Grid.SetRow(c2, 0);
		Grid.SetColumn(c2, 1);
		Grid.SetColumnSpan(c2, 3);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(1000, 100));
		SUT.DesiredSize.Should().Be(new Size(200, 100));
		//SUT.UnclippedDesiredSize.Should().Be(new Size(200, 100));

		SUT.Arrange(new Rect(0, 0, 1000, 100));
	}

	[Fact]
	public void When_Clear_ColumnDefinitions()
	{
		var SUT = new Grid();

		SUT.ColumnDefinitions.Clear();
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Clear();

		SUT.ColumnDefinitions.Should().HaveCount(0);
	}

	[Fact]
	public void When_Zero_Star_Size()
	{
		var SUT = new Grid();
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		SetChild(0, 0);
		SetChild(0, 1);
		SetChild(1, 0);
		SetChild(1, 1);

		SUT.Measure(new Size(800, 800));
		SUT.DesiredSize.Should().Be(new Size(50, 100));

		void SetChild(int col, int row)
		{
			var border = new Border { Height = 50, Width = 50 };
			Grid.SetColumn(border, col);
			Grid.SetRow(border, row);
			SUT.Children.Add(border);
		}
	}

	[Fact]
	public void When_RowSpan_Reuse()
	{
		// This sample is taken from the ToggleSwitch template

		var SUT = new StackPanel();

		var topLevel = new Grid();
		SUT.Children.Add(topLevel);

		topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLengthHelper2.FromValueAndType(10, GridUnitType.Pixel) });
		topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		topLevel.RowDefinitions.Add(new RowDefinition { Height = GridLengthHelper2.FromValueAndType(10, GridUnitType.Pixel) });

		topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthHelper2.FromValueAndType(12, GridUnitType.Pixel), MaxWidth = 12 });
		topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthHelper2.FromValueAndType(12, GridUnitType.Pixel) });
		topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		topLevel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star) });

		var spacer = new Grid() { Margin = new Thickness(0, 5, 0, 5) };
		topLevel.Children.Add(spacer);
		Grid.SetRow(spacer, 1);
		Grid.SetRowSpan(spacer, 3);
		Grid.SetColumnSpan(spacer, 3);

		var knob = new Grid()
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			Width = 20,
			Height = 20
		};

		Grid.SetRow(spacer, 2);
		topLevel.Children.Add(knob);

		SUT.Measure(new Size(800, 800));
		knob.DesiredSize.Should().Be(new Size(20, 20));
	}

	[Fact]
	public void When_One_Child_With_Margin_5()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(5)
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));

		SUT.DesiredSize.Should().Be(new Size(10, 10), because: "SUT.DesiredSize");
#if !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 10), because: "SUT UnclippedDesiredSize");
#endif

		c1.DesiredSize.Should().Be(new Size(10, 10), because: "c1.DesiredSize");

#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(0, 0), because: "c1 UnclippedDesiredSize"); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetAvailableSize(SUT).Should().Be(new Size(20, 20), because: "SUT AvailableSize");
		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 20, 20), because: "SUT LayoutSlot");

#if false
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToArrangeOverride");
#endif
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToMeasureOverride");
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(20, 20), because: "c1 AvailableSize");
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20), because: "c1 LayoutSlot");

		SUT.Children.Should().HaveCount(1);
	}

	[Fact]
	public void When_One_Child_With_Margin_1234()
	{
		var SUT = new Grid
		{
			Name = "test",
			Padding = new Thickness(2)
		};

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(1, 2, 3, 4)
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));

		SUT.DesiredSize.Should().Be(new Size(8, 10), because: "SUT.DesiredSize");
#if !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(8, 10), because: "SUT UnclippedDesiredSize");
#endif

		c1.DesiredSize.Should().Be(new Size(4, 6), because: "c1.DesiredSize");
#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(0, 0), because: "c1 UnclippedDesiredSize"); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetAvailableSize(SUT).Should().Be(new Size(20, 20), because: "SUT AvailableSize");
		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 20, 20), because: "SUT LayoutSlot");

#if false
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(12, 10), because: "c1.SizePassedToArrangeOverride");
#endif

		c1.SizePassedToMeasureOverride.Should().Be(new Size(12, 10), because: "c1.SizePassedToMeasureOverride");
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(16, 16), because: "c1 AvailableSize");
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(2, 2, 16, 16), because: "c1 LayoutSlot");

		SUT.Children.Should().HaveCount(1);
	}

	[Fact]
	public void When_One_Child_With_Margin_1234_Size8()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid
		{
			Name = "test",
			Padding = new Thickness(2)
		};

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(1, 2, 3, 4)
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(8, 8));

		SUT.DesiredSize.Should().Be(new Size(8, 8));
#if CODEBRIX_REFERENCE_API
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(8, 8));
#elif !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(8, 10));
#endif

#if CODEBRIX_REFERENCE_API
		c1.DesiredSize.Should().Be(new Size(4, 4));
#else
		c1.DesiredSize.Should().Be(new Size(4, 6));
#endif
#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(0, 0)); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 8, 8));

		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 8, 8));
		LayoutInformation.GetAvailableSize(SUT).Should().Be(new Size(8, 8));

		c1.SizePassedToArrangeOverride.Should().Be(default(Size));
		c1.SizePassedToMeasureOverride.Should().Be(default(Size));
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(4, 4));
#if CODEBRIX_REFERENCE_API || WINAPPSDK
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(2, 2, 4, 4));
#else
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(2, 2, 4, 6));
#endif

		SUT.Children.Should().HaveCount(1);
	}

	[Fact]
	public void When_One_Child_With_Margin_Center_And_Center()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(0, 0, 0, 30),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Height = 10,
			Width = 10,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		SUT.DesiredSize.Should().Be(new Size(10, 20), because: "SUT.DesiredSize");

#if CODEBRIX_REFERENCE_API
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 20), because: "SUT UnclippedDesiredSize");
#endif

#if CODEBRIX_REFERENCE_API || WINAPPSDK
		c1.DesiredSize.Should().Be(new Size(10, 20), because: "c1.DesiredSize");
#else
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 30), because: "SUT UnclippedDesiredSize");
		c1.DesiredSize.Should().Be(new Size(10, 30), because: "c1.DesiredSize");
#endif

#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(10, 10), because: "c1 UnclippedDesiredSize"); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 50, 50));

#if false
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToArrangeOverride");
#endif

#if CODEBRIX_REFERENCE_API
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToMeasureOverride");
#else
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 0), because: "c1.SizePassedToMeasureOverride");
#endif
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(20, 20), because: "c1 AvailableSize");
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 50, 50), because: "c1 LayoutSlot");

		SUT.Children.Should().HaveCount(1);
	}

	[Fact]
	public void When_One_Child_With_Margin_Center_And_Bottom()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(0, 0, 0, 30),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			Height = 10,
			Width = 10,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));

		SUT.DesiredSize.Should().Be(new Size(10, 20), because: "SUT.DesiredSize");
#if CODEBRIX_REFERENCE_API
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 20), because: "SUT UnclippedDesiredSize");
#elif !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 30), because: "SUT.UnclippedDesiredSize");
#endif

#if CODEBRIX_REFERENCE_API || WINAPPSDK
		c1.DesiredSize.Should().Be(new Size(10, 20), because: "c1.DesiredSize");
#else
		c1.DesiredSize.Should().Be(new Size(10, 30), because: "c1.DesiredSize");
#endif

#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(10, 10), because: "c1 UnclippedDesiredSize"); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 50, 50));

		LayoutInformation.GetAvailableSize(SUT).Should().Be(new Size(20, 20), because: "SUT AvailableSize");
		LayoutInformation.GetLayoutSlot(SUT).Should().Be(new Rect(0, 0, 50, 50), because: "SUT LayoutSlot");

#if false
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToArrangeOverride");
#endif

#if CODEBRIX_REFERENCE_API
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToMeasureOverride");
#else
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 0), because: "c1.SizePassedToMeasureOverride");
#endif
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(20, 20), because: "c1 AvailableSize");
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 50, 50), because: "c1 LayoutSlot");

		SUT.Children.Should().HaveCount(1);
	}

	[Fact]
	public void When_One_Child_With_Margin_Center_And_Top()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(0, 0, 0, 30),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Top,
			Height = 10,
			Width = 10,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));

		SUT.DesiredSize.Should().Be(new Size(10, 20), because: "SUT.DesiredSize");
#if CODEBRIX_REFERENCE_API
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 20), because: "SUT UnclippedDesiredSize");
		c1.DesiredSize.Should().Be(new Size(10, 20), because: "c1.DesiredSize");
#elif !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(10, 30), because: "SUT UnclippedDesiredSize");
		c1.DesiredSize.Should().Be(new Size(10, 30), because: "c1.DesiredSize");
#endif

#if !WINAPPSDK
		GetUnclippedDesiredSize(c1).Should().Be(new Size(10, 10), because: "c1 UnclippedDesiredSize"); // UnclippedDesiredSize excludes margins
#endif

		SUT.Arrange(new Rect(0, 0, 50, 50));

#if false
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToArrangeOverride");
#endif

#if CODEBRIX_REFERENCE_API
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 10), because: "c1.SizePassedToMeasureOverride");
#else
		c1.SizePassedToMeasureOverride.Should().Be(new Size(10, 0), because: "c1.SizePassedToMeasureOverride");
#endif
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(20, 20), because: "c1 AvailableSize");
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 50, 50), because: "c1 LayoutSlot");

		SUT.Children.Should().HaveCount(1);
	}

	[Fact]
	public void When_One_Fixed_Size_Child_With_Margin_Right_And_Stretch()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View
		{
			Name = "Child01",
			Margin = new Thickness(0, 0, 50, 0),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Height = 50,
			Width = 50,
		};
		Grid.SetRow(c1, 0);
		Grid.SetColumn(c1, 1);

		SUT.Children.Add(c1);

		SUT.Measure(new Size(300, 300));
		SUT.DesiredSize.Should().Be(new Size(100, 50), because: "SUT.DesiredSize");
		c1.DesiredSize.Should().Be(new Size(100, 50), because: "c1.DesiredSize");
#if !WINAPPSDK
		GetUnclippedDesiredSize(SUT).Should().Be(new Size(100, 50), because: "SUT UnclippedDesiredSize");
#endif

		SUT.Arrange(new Rect(0, 0, 300, 300));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(100, 0, 100, 300), because: "c1 LayoutSlot");
		LayoutInformation.GetAvailableSize(c1).Should().Be(new Size(double.PositiveInfinity, 300), because: "c1 AvailableSize");
#if false
		c1.SizePassedToArrangeOverride.Should().Be(new Size(0, 0), because: "c1.SizePassedToArrangeOverride");
#else
		c1.SizePassedToArrangeOverride.Should().Be(new Size(50, 50), because: "c1.SizePassedToArrangeOverride");
#endif
		c1.SizePassedToMeasureOverride.Should().Be(new Size(50, 50), because: "c1.SizePassedToMeasureOverride");
		SUT.Children.Should().HaveCount(1);
	}

	/// <summary>
	/// The unclipped desired size the layouter recorded for <paramref name="element"/> - what it
	/// asked for before its own slot clipped it.
	/// </summary>
	/// <remarks>
	/// The framework keeps this on an internal Layouter reached through an internal interface, and
	/// this suite deliberately does not ask the framework to widen either with an InternalsVisibleTo
	/// grant for a test-only concern - the same decision DispatcherInitializer records. Reflection is
	/// how the original test reached it too; it simply had the grant and could name the types.
	/// </remarks>
	/// <param name="element">The element whose recorded size is wanted.</param>
	/// <returns>The unclipped desired size, which excludes margins.</returns>
	/// <exception cref="InvalidOperationException">
	/// The framework no longer keeps the value where this expects it, so the accessor needs updating
	/// to match.
	/// </exception>
	private static Size GetUnclippedDesiredSize(UIElement element)
	{
		//The original chose between these two shapes with #if CODEBRIX_REFERENCE_API. Which one the
		//framework assembly actually has is a property of how it was BUILT, not of how this test
		//project was compiled, so the choice is made here at run time instead. MEASURED: the
		//CodeBrix.Platform.UI.dll this suite loads carries UIElement.m_unclippedDesiredSize and has
		//no FrameworkElement._layouter at all; the second branch is kept so a differently built
		//framework still works rather than failing mysteriously.
		if (FindField(element.GetType(), "m_unclippedDesiredSize") is { } direct)
		{
			return (Size)direct.GetValue(element)!;
		}

		var layouter = FindField(element.GetType(), "_layouter")?.GetValue(element);
		var viaLayouter = layouter is null ? null : FindField(layouter.GetType(), "_unclippedDesiredSize");

		if (viaLayouter is null)
		{
			throw new InvalidOperationException(
				"Could not read the unclipped desired size: the framework has neither "
				+ "UIElement.m_unclippedDesiredSize nor FrameworkElement._layouter."
				+ "_unclippedDesiredSize. The suite's accessor needs updating to match it.");
		}

		return (Size)viaLayouter.GetValue(layouter)!;
	}

	/// <summary>
	/// Finds a non-public instance field by name on <paramref name="type"/> or any of its bases.
	/// </summary>
	/// <param name="type">The concrete runtime type to start from.</param>
	/// <param name="name">The field's name.</param>
	/// <returns>The field, or null if no type in the hierarchy declares it.</returns>
	private static FieldInfo FindField(Type type, string name)
	{
		for (var t = type; t is not null; t = t.BaseType)
		{
			var field = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
			if (field is not null)
			{
				return field;
			}
		}

		return null;
	}

	[Fact]
	public void When_One_Auto_Columns_and_one_star_and_two_children()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10) };
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(15, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(5, 5));
		c2.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 5, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(5, 0, 15, 20));

		SUT.Children.Count.Should().Be(2);
	}

	[Fact]
	public void When_Two_Auto_Columns_two_children()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10) };
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(15, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(5, 5));
		c2.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 5, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(5, 0, 10, 20));

		SUT.Children.Count().Should().Be(2);
	}

	[Fact]
	public void When_One_Auto_and_one_abs_and_one_star_and_three_children()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(10, 10) };
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(10, 7) };
		Grid.SetColumn(c3, 2);
		SUT.Children.Add(c3);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;

		measuredSize.Should().Be(new Size(20, 10));
		c1.DesiredSize.Should().Be(new Size(5, 5));
		c2.DesiredSize.Should().Be(new Size(6, 10));
		c3.DesiredSize.Should().Be(new Size(9, 7));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 5, 20));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(5, 0, 6, 20));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(11, 0, 9, 20));

		SUT.Children.Should().HaveCount(3);
	}

	[Fact]
	public void When_Nine_grid_and_one_auto_cell_and_three_children()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(3, 4) };
		Grid.SetRow(c2, 1);
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(8, 9) };
		Grid.SetRow(c3, 2);
		Grid.SetColumn(c3, 2);
		SUT.Children.Add(c3);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(16, 17));

		c1.DesiredSize.Should().Be(new Size(5, 5));
		c2.DesiredSize.Should().Be(new Size(3, 4));
		c3.DesiredSize.Should().Be(new Size(8, 8));

		SUT.Arrange(new Rect(0, 0, 20, 20));

#if false
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 8.5f, 8.0));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(8.5f, 8.0f, 3, 4));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(11.5f, 12.0f, 8.5f, 8.0f));
#else
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 8.0f, 8.0f));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(8.0f, 8.0f, 3, 4));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(11.0f, 12.0f, 9.0f, 8.0f));
#endif
		SUT.Children.Should().HaveCount(3);
	}

	[Fact]
	public void When_Quad_two_auto_and_four_children()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(3, 4) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(4, 5) };
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(6, 7) };
		Grid.SetRow(c3, 1);
		SUT.Children.Add(c3);

		var c4 = new View { Name = "Child04", RequestedDesiredSize = new Size(8, 9) };
		Grid.SetRow(c4, 1);
		Grid.SetColumn(c4, 1);
		SUT.Children.Add(c4);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(14, 14));

		c1.RequestedDesiredSize.Should().Be(new Size(3, 4));
		c2.RequestedDesiredSize.Should().Be(new Size(4, 5));
		c3.RequestedDesiredSize.Should().Be(new Size(6, 7));
		c4.RequestedDesiredSize.Should().Be(new Size(8, 9));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 12, 5));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(12, 0, 8, 5));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(0, 5, 12, 15));
		LayoutInformation.GetLayoutSlot(c4).Should().Be(new Rect(12, 5, 8, 15));

		SUT.Children.Count.Should().Be(4);
	}

	[Fact]
	public void When_Nine_grid_and_one_auto_cell_and_four_children()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(5, 5) };
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(3, 4) };
		Grid.SetRow(c2, 1);
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(8, 9) };
		Grid.SetRow(c3, 2);
		Grid.SetColumn(c3, 2);
		SUT.Children.Add(c3);

		var c4 = new View { Name = "Child04", RequestedDesiredSize = new Size(5, 5) };
		Grid.SetRow(c4, 1);
		SUT.Children.Add(c4);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(16, 19));

		c1.DesiredSize.Should().Be(new Size(5, 5));
		c2.DesiredSize.Should().Be(new Size(3, 4));
		c3.DesiredSize.Should().Be(new Size(8, 9));
		c4.DesiredSize.Should().Be(new Size(5, 5));

		SUT.Arrange(new Rect(0, 0, 20, 20));

#if false
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 8.5f, 5.5f));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(8.5f, 5.5f, 3, 5.5f));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(11.5f, 11, 8.5f, 9));
		LayoutInformation.GetLayoutSlot(c4).Should().Be(new Rect(0, 5.5f, 8.5f, 5.5f));

#else
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 8.0f, 6.0f));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(8.0f, 6.0f, 3, 5.0f));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(11.0f, 11, 9.0f, 9));
		LayoutInformation.GetLayoutSlot(c4).Should().Be(new Rect(0, 6.0f, 8.0f, 5.0f));
#endif

		SUT.Children.Should().HaveCount(4);
	}

	[Fact]
	public void When_Three_Rows_One_Auto_Two_Fixed_And_Row_Span_Full()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test", Height = 44 };

		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		SUT.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(0, 10) };
		Grid.SetRow(c1, 1);
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(30, 0) };
		Grid.SetRow(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(8, 24) };
		Grid.SetRowSpan(c3, 3);
		SUT.Children.Add(c3);

		SUT.Measure(new Size(100, 44));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(30, 44));

		c1.DesiredSize.Should().Be(new Size(0, 10));
		c2.DesiredSize.Should().Be(new Size(30, 0));
		c3.DesiredSize.Should().Be(new Size(8, 24));

		SUT.Arrange(new Rect(0, 0, 20, 44));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 18, 30, 10));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(0, 18, 30, 10));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(0, 0, 30, 46));

		SUT.Children.Should().HaveCount(3);
	}

	[Fact]
	public void When_Three_Colums_One_Auto_Two_Fixed_And_Column_Span_Full()
	{
		using var _ = new AssertionScope();

		var SUT = new Grid() { Name = "test", Width = 44 };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });

		var c1 = new View { Name = "Child01", RequestedDesiredSize = new Size(10, 0) };
		Grid.SetColumn(c1, 1);
		SUT.Children.Add(c1);

		var c2 = new View { Name = "Child02", RequestedDesiredSize = new Size(0, 30) };
		Grid.SetColumn(c2, 1);
		SUT.Children.Add(c2);

		var c3 = new View { Name = "Child03", RequestedDesiredSize = new Size(24, 8) };
		Grid.SetColumnSpan(c3, 3);
		SUT.Children.Add(c3);

		SUT.Measure(new Size(44, 100));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(44, 30));

		c1.DesiredSize.Should().Be(new Size(10, 0));
		c2.DesiredSize.Should().Be(new Size(0, 30));
		c3.DesiredSize.Should().Be(new Size(24, 8));

		SUT.Arrange(new Rect(0, 0, 44, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(18, 0, 10, 30));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(18, 0, 10, 30));
		LayoutInformation.GetLayoutSlot(c3).Should().Be(new Rect(0, 0, 46, 30));

		SUT.Children.Should().HaveCount(3);
	}

	[Fact]
	public void When_One_Child_With_VerticalTopAlignment_and_Fixed_Height()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Height = 10,
			VerticalAlignment = VerticalAlignment.Top,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.RenderSize.Should().Be(new Size(20, 10));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_With_VerticalBottomAlignment_and_Fixed_Height()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Height = 10,
			VerticalAlignment = VerticalAlignment.Bottom,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.ActualOffset.Should().Be(new Vector3(0, 10, 0));
		c1.RenderSize.Should().Be(new Size(20, 10));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_With_VerticalCenterAlignment_and_Fixed_Height()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Height = 10,
			VerticalAlignment = VerticalAlignment.Center,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.ActualOffset.Should().Be(new Vector3(0, 5, 0));
		c1.RenderSize.Should().Be(new Size(20, 10));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_With_HorizontalLeftAlignment_and_Fixed_Width()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Width = 10,
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.ActualOffset.Should().Be(Vector3.Zero);
		c1.RenderSize.Should().Be(new Size(10, 20));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_With_HorizontalRightAlignment_and_Fixed_Width()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Width = 10,
			HorizontalAlignment = HorizontalAlignment.Right,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.ActualOffset.Should().Be(new Vector3(10, 0, 0));
		c1.RenderSize.Should().Be(new Size(10, 20));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_With_HorizontalCenterAlignment_and_Fixed_Width()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			Width = 10,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.ActualOffset.Should().Be(new Vector3(5, 0, 0));
		c1.RenderSize.Should().Be(new Size(10, 20));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_With_VerticalTopAlignment_and_Variable_Height()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			VerticalAlignment = VerticalAlignment.Top,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.RenderSize.Should().Be(new Size(20, 10));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_With_VerticalCenterAlignment_and_Variable_Height()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			VerticalAlignment = VerticalAlignment.Center,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.ActualOffset.Should().Be(new Vector3(0, 5, 0));
		c1.RenderSize.Should().Be(new Size(20, 10));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_With_HorizontalStretchAlignment_and_MaxWidth()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			MaxWidth = 10,
		};
		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.ActualOffset.Should().Be(new Vector3(5, 0, 0));
		c1.RenderSize.Should().Be(new Size(10, 20));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_With_VerticalStretchAlignment_and_MaxHeight()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			MaxHeight = 10,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.ActualOffset.Should().Be(new Vector3(0, 5, 0));
		c1.RenderSize.Should().Be(new Size(20, 10));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_With_VerticalCenterAlignment_HorizontalCenterAlignment_and_Variable_Height_and_Variable_Width()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			RequestedDesiredSize = new Size(10, 10),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(20, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(10, 10));
		c1.RequestedDesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 20));
		c1.ActualOffset.Should().Be(new Vector3(5, 5, 0));
		c1.RenderSize.Should().Be(new Size(10, 10));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_Centered_And_Auto_row_And_Fixed_Column()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var c1 = new View
		{
			Name = "Child01",

			// The selector will return a different size if the measured size if greater than the column size.
			// This should not happen, as the child should not be measured with a Width greater
			// than 20, as the column is of size 20.
			DesiredSizeSelector = s => s.Width > 20 ? new Size(20, 5) : new Size(10, 10),

			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(100, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(20, 10));
		c1.DesiredSize.Should().Be(new Size(10, 10));

		SUT.Arrange(new Rect(0, 0, 100, 20));

		c1.DesiredSize.Should().Be(new Size(10, 10));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 20, 10));
		c1.ActualOffset.Should().Be(new Vector3(5, 0, 0));
		c1.RenderSize.Should().Be(new Size(10, 10));

		SUT.Children.Count.Should().Be(1);
	}

	[Fact]
	public void When_One_Child_Centered_And_Auto_row_And_Star_Column()
	{
		var SUT = new Grid() { Name = "test" };

		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		SUT.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		SUT.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var c1 = new View
		{
			Name = "Child01",

			// The selector will return a different size if the measured size if greater than the column size.
			// This should not happen, as the child should not be measured with a Width greater
			// than 20, as the column is of size 20.
			DesiredSizeSelector = s => s.Width > 20 ? new Size(20, 5) : new Size(10, 10),

			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		Grid.SetColumn(c1, 1);
		SUT.Children.Add(c1);

		var c2 = new View
		{
			Name = "Child02",
			RequestedDesiredSize = new Size(11, 11),
		};

		SUT.Children.Add(c2);

		SUT.Measure(new Size(100, 20));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(31, 11));
		c1.DesiredSize.Should().Be(new Size(20, 5));
		c2.RequestedDesiredSize.Should().Be(new Size(11, 11));
		c1.MeasureCallCount.Should().Be(1);
		c2.MeasureCallCount.Should().Be(1); // The measure count is 1 beceause the grid has a recognized pattern (Nx1). It would be 2 otherwise.
		c1.ArrangeCallCount.Should().Be(0);
		c2.ArrangeCallCount.Should().Be(0);

		SUT.Arrange(new Rect(0, 0, 100, 20));

		c1.DesiredSize.Should().Be(new Size(20, 5));
		c2.RequestedDesiredSize.Should().Be(new Size(11, 11));
		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(11, 0, 89, 11));
#if false
		c1.ActualOffset.Should().Be(new Vector3(45.5f, 3, 0));
#else
		c1.ActualOffset.Should().Be(new Vector3(46, 3, 0));
#endif
		c1.RenderSize.Should().Be(new Size(20, 5));
		LayoutInformation.GetLayoutSlot(c2).Should().Be(new Rect(0, 0, 11, 11));
		c1.MeasureCallCount.Should().Be(1, "c1.MeasureCallCount");
		c2.MeasureCallCount.Should().Be(1, "c2.MeasureCallCount"); // The measure count is 1 because the grid has a recognized pattern (Nx1). It would be 2 otherwise.
#if false
		c1.ArrangeCallCount.Should().Be(0, "c1.ArrangeCallCount");
		c2.ArrangeCallCount.Should().Be(0, "c2.ArrangeCallCount");
#else
		c1.ArrangeCallCount.Should().Be(1, "c1.ArrangeCallCount");
		c2.ArrangeCallCount.Should().Be(1, "c2.ArrangeCallCount");
#endif

		SUT.Children.Count.Should().Be(2);
	}

	[Fact]
	public void When_One_Child_and_Measure_Bigger_than_arrange()
	{
		var SUT = new Grid() { Name = "test" };

		var c1 = new View
		{
			Name = "Child01",
			DesiredSizeSelector = s => s,
		};

		SUT.Children.Add(c1);

		SUT.Measure(new Size(100, 100));
		var measuredSize = SUT.DesiredSize;
		measuredSize.Should().Be(new Size(100, 100));
		c1.DesiredSize.Should().Be(new Size(100, 100));

		SUT.Arrange(new Rect(0, 0, 20, 20));

		LayoutInformation.GetLayoutSlot(c1).Should().Be(new Rect(0, 0, 100, 100));

		SUT.Children.Count.Should().Be(1);
	}
}
