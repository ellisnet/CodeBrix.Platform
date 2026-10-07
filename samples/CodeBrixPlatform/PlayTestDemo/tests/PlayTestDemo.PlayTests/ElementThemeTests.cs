using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Windows.UI;
using Xunit;

namespace PlayTestDemo.PlayTests;

/// <summary>An element-level RequestedTheme re-themes its subtree: plain text and the controls inside a Border whose
/// theme is the opposite of the application's draw with that theme's brushes, while their siblings outside keep the
/// application's theme.</summary>
public sealed partial class ApplicationTests
{
    private sealed class ThemeProbe
    {
        public Border Themed;
        public TextBlock InsideText, OutsideText;
        public CheckBox InsideCheck, OutsideCheck;
        public Button InsideButton, OutsideButton;
        public TextBox InsideBox, OutsideBox;
    }

    private static double Luminance(Brush brush)
    {
        var color = ((SolidColorBrush)brush).Color;
        return (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;
    }

    private static Color ColorOf(Brush brush) => ((SolidColorBrush)brush).Color;

    private static StackPanel ThemeColumn(ThemeProbe probe, bool inside, string suffix)
    {
        var text = new TextBlock { Text = "Plain text " + suffix };
        var check = new CheckBox { Content = "Check " + suffix };
        var button = new Button { Content = "Button " + suffix };
        var box = new TextBox { Text = "Box " + suffix, Width = 240 };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(text, "ThemeText" + suffix);
        if (inside) (probe.InsideText, probe.InsideCheck, probe.InsideButton, probe.InsideBox) = (text, check, button, box);
        else (probe.OutsideText, probe.OutsideCheck, probe.OutsideButton, probe.OutsideBox) = (text, check, button, box);
        return new StackPanel { Spacing = 12, Padding = new Thickness(24), Children = { text, check, button, box } };
    }

    private static ElementTheme Opposite(ApplicationTheme theme) =>
        theme == ApplicationTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;

    [Fact]
    public async Task An_element_requested_theme_restyles_plain_text_and_controls_in_its_subtree()
    {
        var probe = new ThemeProbe();
        var appTheme = await Page.EvaluateAsync(() => Application.Current.RequestedTheme);
        var subtreeTheme = Opposite(appTheme);

        await Page.SetContentAsync(() =>
        {
            probe.Themed = new Border
            {
                RequestedTheme = subtreeTheme,
                Background = new SolidColorBrush(subtreeTheme == ElementTheme.Dark ? Microsoft.UI.Colors.Black : Microsoft.UI.Colors.White),
                Child = ThemeColumn(probe, inside: true, "Inside"),
            };
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { ThemeColumn(probe, inside: false, "Outside"), probe.Themed },
            };
        });
        await Expect(Page.GetByTestId("ThemeTextInside")).ToBeVisibleAsync();

        var measured = await Page.EvaluateAsync(() => new
        {
            InsideText = Luminance(probe.InsideText.Foreground),
            OutsideText = Luminance(probe.OutsideText.Foreground),
            InsideCheck = Luminance(probe.InsideCheck.Foreground),
            OutsideCheck = Luminance(probe.OutsideCheck.Foreground),
            InsideButton = Luminance(probe.InsideButton.Foreground),
            OutsideButton = Luminance(probe.OutsideButton.Foreground),
            InsideBox = Luminance(probe.InsideBox.Foreground),
            OutsideBox = Luminance(probe.OutsideBox.Foreground),
            InsideButtonBackground = ColorOf(probe.InsideButton.Background),
            OutsideButtonBackground = ColorOf(probe.OutsideButton.Background),
            InsideBoxBackground = ColorOf(probe.InsideBox.Background),
            OutsideBoxBackground = ColorOf(probe.OutsideBox.Background),
        });

        // Dark-theme foregrounds are light (luminance near 1), light-theme foregrounds are dark (near 0).
        var darkInside = subtreeTheme == ElementTheme.Dark;
        foreach (var (inside, outside) in new[]
        {
            (measured.InsideText, measured.OutsideText),
            (measured.InsideCheck, measured.OutsideCheck),
            (measured.InsideButton, measured.OutsideButton),
            (measured.InsideBox, measured.OutsideBox),
        })
        {
            if (darkInside)
            {
                inside.Should().BeGreaterThan(0.8);
                outside.Should().BeLessThan(0.2);
            }
            else
            {
                inside.Should().BeLessThan(0.2);
                outside.Should().BeGreaterThan(0.8);
            }
        }

        measured.InsideButtonBackground.Should().NotBe(measured.OutsideButtonBackground);
        measured.InsideBoxBackground.Should().NotBe(measured.OutsideBoxBackground);

        // And on screen: the plain text inside the themed Border is drawn in that theme's text colour.
        var png = await Page.GetByTestId("ThemeTextInside").ScreenshotAsync();
        using var bitmap = SkiaSharp.SKBitmap.Decode(png);
        double darkest = 1, brightest = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                var luminance = (0.2126 * pixel.Red + 0.7152 * pixel.Green + 0.0722 * pixel.Blue) / 255.0;
                darkest = System.Math.Min(darkest, luminance);
                brightest = System.Math.Max(brightest, luminance);
            }
        }
        if (darkInside) brightest.Should().BeGreaterThan(0.7);
        else darkest.Should().BeLessThan(0.3);

        var themes = await Page.EvaluateAsync(() => (
            probe.Themed.ActualTheme, probe.InsideText.ActualTheme, probe.InsideCheck.ActualTheme, probe.OutsideText.ActualTheme));
        themes.Should().Be((subtreeTheme, subtreeTheme, subtreeTheme, appTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light));
    }

    [Fact]
    public async Task Clearing_an_element_requested_theme_returns_its_subtree_to_the_application_theme()
    {
        var probe = new ThemeProbe();
        var appTheme = await Page.EvaluateAsync(() => Application.Current.RequestedTheme);
        var subtreeTheme = Opposite(appTheme);

        await Page.SetContentAsync(() => new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                ThemeColumn(probe, inside: false, "Outside"),
                (probe.Themed = new Border { RequestedTheme = subtreeTheme, Child = ThemeColumn(probe, inside: true, "Inside") }),
            },
        });
        await Expect(Page.GetByTestId("ThemeTextInside")).ToBeVisibleAsync();

        await Page.EvaluateAsync(() => { probe.Themed.RequestedTheme = ElementTheme.Default; });

        var after = await Page.EvaluateAsync(() => (
            probe.InsideText.ActualTheme == probe.OutsideText.ActualTheme,
            ColorOf(probe.InsideText.Foreground) == ColorOf(probe.OutsideText.Foreground),
            ColorOf(probe.InsideCheck.Foreground) == ColorOf(probe.OutsideCheck.Foreground),
            ColorOf(probe.InsideButton.Background) == ColorOf(probe.OutsideButton.Background),
            ColorOf(probe.InsideBox.Background) == ColorOf(probe.OutsideBox.Background)));
        after.Should().Be((true, true, true, true, true));
    }
}
