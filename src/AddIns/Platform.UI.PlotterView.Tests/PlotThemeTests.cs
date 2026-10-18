#nullable enable

using CodeBrix.Platform.UI.PlotterView.Engine;
using CodeBrix.Plotter;
using CodeBrix.Plotter.Axes;
using CodeBrix.Plotter.Legends;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.PlotterView.Tests;

public class PlotThemeTests
{
    private static readonly PlotterColor _darkGrid = PlotterColor.FromArgb(0x40, 0xFF, 0xFF, 0xFF);
    private static readonly PlotterColor _darkMinorGrid = PlotterColor.FromArgb(0x20, 0xFF, 0xFF, 0xFF);

    [Fact]
    public void the_light_theme_leaves_every_plotter_default_as_it_was()
    {
        //Arrange
        var (model, axis, legend) = CreateModel();
        var theme = new PlotTheme { IsDark = false };

        //Act
        theme.Apply(model);

        //Assert
        model.TextColor.Should().Be(PlotterColors.Black);
        model.PlotAreaBorderColor.Should().Be(PlotterColors.Black);
        axis.AxislineColor.Should().Be(PlotterColors.Black);
        axis.TicklineColor.Should().Be(PlotterColors.Black);
        axis.MajorGridlineColor.Should().Be(PlotterColor.FromArgb(0x40, 0, 0, 0));
        axis.MinorGridlineColor.Should().Be(PlotterColor.FromArgb(0x20, 0, 0, 0));
        axis.ExtraGridlineColor.Should().Be(PlotterColors.Black);
        legend.SeriesInvisibleTextColor.Should().Be(new Legend().SeriesInvisibleTextColor);
    }

    [Fact]
    public void the_dark_theme_gives_every_colour_the_app_did_not_set_its_light_on_dark_twin()
    {
        //Arrange
        var (model, axis, legend) = CreateModel();
        var theme = new PlotTheme { IsDark = true };

        //Act
        theme.Apply(model);

        //Assert
        model.TextColor.Should().Be(PlotterColors.White);
        model.PlotAreaBorderColor.Should().Be(PlotterColors.White);
        axis.AxislineColor.Should().Be(PlotterColors.White);
        axis.TicklineColor.Should().Be(PlotterColors.White);
        axis.MajorGridlineColor.Should().Be(_darkGrid);
        axis.MinorGridlineColor.Should().Be(_darkMinorGrid);
        axis.ExtraGridlineColor.Should().Be(PlotterColors.White);
        legend.SeriesInvisibleTextColor.Should().Be(PlotterColor.FromArgb(64, 0xFF, 0xFF, 0xFF));
    }

    [Fact]
    public void the_automatic_text_colours_are_never_written_and_resolve_through_the_themed_text_colour()
    {
        //Arrange
        var (model, axis, legend) = CreateModel();
        var theme = new PlotTheme { IsDark = true };

        //Act
        theme.Apply(model);

        //Assert
        model.TitleColor.Should().Be(PlotterColors.Automatic);
        model.SubtitleColor.Should().Be(PlotterColors.Automatic);
        axis.TextColor.Should().Be(PlotterColors.Automatic);
        axis.TitleColor.Should().Be(PlotterColors.Automatic);
        legend.LegendTextColor.Should().Be(PlotterColors.Automatic);
        model.TitleColor.GetActualColor(model.TextColor).Should().Be(PlotterColors.White);
        legend.LegendTextColor.GetActualColor(model.TextColor).Should().Be(PlotterColors.White);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void a_colour_the_app_set_is_never_changed_in_either_theme(bool dark)
    {
        //Arrange
        var (model, axis, _) = CreateModel();
        model.TextColor = PlotterColors.Red;
        model.PlotAreaBorderColor = PlotterColors.Green;
        axis.AxislineColor = PlotterColors.Blue;
        axis.MajorGridlineColor = PlotterColors.Orange;
        var theme = new PlotTheme { IsDark = dark };

        //Act
        theme.Apply(model);

        //Assert
        model.TextColor.Should().Be(PlotterColors.Red);
        model.PlotAreaBorderColor.Should().Be(PlotterColors.Green);
        axis.AxislineColor.Should().Be(PlotterColors.Blue);
        axis.MajorGridlineColor.Should().Be(PlotterColors.Orange);
        axis.TicklineColor.Should().Be(dark ? PlotterColors.White : PlotterColors.Black, "the colours left alone still follow the theme");
    }

    [Fact]
    public void a_white_the_app_set_under_the_light_theme_survives_a_trip_to_dark_and_back()
    {
        //Arrange
        var (model, _, _) = CreateModel();
        model.TextColor = PlotterColors.White;
        var theme = new PlotTheme { IsDark = true };

        //Act
        theme.Apply(model);
        theme.IsDark = false;
        theme.Apply(model);

        //Assert
        model.TextColor.Should().Be(PlotterColors.White, "the app's own white is not the theme's to take back");
    }

    [Fact]
    public void returning_to_the_light_theme_puts_back_the_plotter_defaults()
    {
        //Arrange
        var (model, axis, _) = CreateModel();
        var theme = new PlotTheme { IsDark = true };
        theme.Apply(model);

        //Act
        theme.IsDark = false;
        theme.Apply(model);

        //Assert
        model.TextColor.Should().Be(PlotterColors.Black);
        axis.AxislineColor.Should().Be(PlotterColors.Black);
        axis.MajorGridlineColor.Should().Be(PlotterColor.FromArgb(0x40, 0, 0, 0));
    }

    [Fact]
    public void a_colour_the_app_changes_while_dark_is_kept_when_the_light_theme_returns()
    {
        //Arrange
        var (model, axis, _) = CreateModel();
        var theme = new PlotTheme { IsDark = true };
        theme.Apply(model);
        model.TextColor = PlotterColors.Yellow;

        //Act
        theme.IsDark = false;
        theme.Apply(model);
        theme.IsDark = true;
        theme.Apply(model);

        //Assert
        model.TextColor.Should().Be(PlotterColors.Yellow);
        axis.AxislineColor.Should().Be(PlotterColors.White);
    }

    [Fact]
    public void an_axis_added_after_the_first_paint_follows_the_theme_too()
    {
        //Arrange
        var (model, _, _) = CreateModel();
        var theme = new PlotTheme { IsDark = true };
        theme.Apply(model);
        var later = new LinearAxis { Position = AxisPosition.Left };
        model.Axes.Add(later);

        //Act
        theme.Apply(model);

        //Assert
        later.AxislineColor.Should().Be(PlotterColors.White);
        later.TicklineColor.Should().Be(PlotterColors.White);
    }

    private static (PlotModel Model, LinearAxis Axis, Legend Legend) CreateModel()
    {
        var model = new PlotModel { Title = "Theme", Subtitle = "fence" };
        var axis = new LinearAxis { Position = AxisPosition.Bottom, Title = "x" };
        var legend = new Legend();
        model.Axes.Add(axis);
        model.Legends.Add(legend);
        return (model, axis, legend);
    }
}
