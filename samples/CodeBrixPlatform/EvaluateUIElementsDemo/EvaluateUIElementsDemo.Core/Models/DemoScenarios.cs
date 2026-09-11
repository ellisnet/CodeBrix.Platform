using System.Collections.Generic;
using System.Linq;

namespace EvaluateUIElementsDemo.Models;

/// <summary>
/// The catalogue of scenarios the application offers, one per finding of the UI requirements
/// frame review that was rated "suspicious". The numbers are stable: an evaluation is reported
/// by number, so entries are only ever appended.
/// </summary>
public static class DemoScenarios
{
    /// <summary>The view keys the UI knows how to build.</summary>
    public static class Views
    {
        public const string TextBox = "TextBox";
        public const string PasswordBox = "PasswordBox";
        public const string ProgressRing = "ProgressRing";
        public const string NavigationView = "NavigationView";
        public const string SplitView = "SplitView";
        public const string ImageUniformToFill = "ImageUniformToFill";
        public const string ToggleSwitch = "ToggleSwitch";
        public const string ToggleButton = "ToggleButton";
    }

    /// <summary>Every scenario, in list order.</summary>
    public static IReadOnlyList<DemoScenario> All { get; } = new[]
    {
        new DemoScenario
        {
            Number = 1,
            Title = "TextBox with keyboard focus",
            Summary = "A TextBox given Foreground Blue and BorderThickness 0: what WinUI's template does to it while focused or under the pointer, and how an application keeps its own look.",
            DialogText =
                "A TextBox styled with Foreground Blue and BorderThickness 0.\n"
                + "WinUI's own TextBox template swaps in theme brushes while the box has focus (dark "
                + "text, a thin border, a 2 px accent underline) and while the pointer is over it (dark "
                + "text on a grey fill). Boxes A and B show that. Box D overrides those theme resources, "
                + "which is the WinUI way to keep your own look; box E overrides only the pointer-over ones.",
            Instructions =
                "Tap into box A and type; press Tab to reach B and type; then do the same in D and in E, each time "
                + "pressing Tab while the pointer stays over the box you were typing in.\n"
                + "Look for: A and B are EXPECTED to show dark text with a thin border and an accent underline "
                + "while focused, and dark text on a grey fill while the pointer is over them, returning to Blue "
                + "on White once neither is true - that is WinUI's template, not a platform fault. D must stay "
                + "Blue on White with no border or underline in every state. E must look like A while focused "
                + "(dark text, border, underline) and go back to Blue on White the moment it loses focus, whether "
                + "the pointer is over it or not. C is the default style.",
            ViewKey = Views.TextBox,
            IsVerified = true,
        },
        new DemoScenario
        {
            Number = 2,
            Title = "PasswordBox with keyboard focus",
            Summary = "A PasswordBox given Foreground Blue and BorderThickness 0: the masking characters and the chrome while focused or under the pointer, and how an application keeps its own look.",
            DialogText =
                "A PasswordBox styled with Foreground Blue and BorderThickness 0.\n"
                + "WinUI's own PasswordBox template swaps in the same theme brushes as the TextBox "
                + "template while the box has focus or the pointer is over it, so the bullets go dark "
                + "and a border and underline appear. Boxes A and B show that. Box D overrides those "
                + "theme resources, which is the WinUI way to keep your own look.",
            Instructions =
                "Tap into box A and type a few characters; do the same in box B, whose PasswordChar is #; then tap "
                + "into box D, type, and press Tab while the pointer stays over D. Move the pointer over each box too.\n"
                + "Look for: A and B are EXPECTED to show dark bullets (or # characters) with a thin border and an "
                + "accent underline while focused, and dark bullets on a grey fill while the pointer is over them, "
                + "returning to Blue on White once neither is true - that is WinUI's template, not a platform fault. "
                + "D must stay Blue on White with no border or underline in every state. C is the default style.\n"
                + "The default masking character is the bullet (U+2022) on every head, drawn from the application's own "
                + "font; that is an intentional divergence from WinUI's black circle (U+25CF). A default-style PasswordBox "
                + "must be exactly as tall as a default-style TextBox of the same FontSize, empty or full.",
            ViewKey = Views.PasswordBox,
            IsVerified = true,
        },
        new DemoScenario
        {
            Number = 3,
            Title = "ProgressRing",
            Summary = "An active ProgressRing: an animated ring, in an application that references the Lottie add-in.",
            DialogText =
                "An active ProgressRing.\n"
                + "The survey's test suite deliberately has no Lottie add-in, and there the ring painted a "
                + "red UNOX0001 diagnostic line instead of a ring. This application references the add-in, "
                + "the way a real application must.",
            Instructions =
                "Switch the ring on and off with the toggle.\n"
                + "Look for: an animated indeterminate ring while active and nothing at all while inactive; the "
                + "small red ring should follow its Foreground; the determinate ring should show a fixed 65 percent "
                + "arc. No red diagnostic text should appear anywhere.",
            ViewKey = Views.ProgressRing,
        },
        new DemoScenario
        {
            Number = 4,
            Title = "NavigationView with its pane shut",
            Summary = "A NavigationView in Left mode: what the 48-pixel compact strip shows once the pane is shut.",
            DialogText =
                "A NavigationView with two menu items, PaneDisplayMode Left.\n"
                + "In the survey, shutting the pane left both item labels drawn at full size inside the "
                + "48-pixel strip, so 'Alpha' was sliced off where the page began.",
            Instructions =
                "Shut the pane with the hamburger button (or the Close pane button), then open it again. Tap Alpha "
                + "and Beta to swap the page.\n"
                + "Look for: with the pane shut, the compact strip should show icons only (view B) or nothing "
                + "sensible for items that have no icon (view A), never a label chopped off mid-letter. The page "
                + "should take the room the pane gave up.",
            ViewKey = Views.NavigationView,
        },
        new DemoScenario
        {
            Number = 5,
            Title = "SplitView with an Overlay pane",
            Summary = "A SplitView whose pane lies over the content: is the content dimmed while the pane is open?",
            DialogText =
                "A SplitView with a Red pane over Blue content, DisplayMode Overlay.\n"
                + "In the survey, opening the pane dimmed the Blue content to navy under a 50 percent black "
                + "scrim nobody asked for. The Inline mode did not dim it.",
            Instructions =
                "Open and close the pane. Try each DisplayMode, and each LightDismissOverlayMode.\n"
                + "Look for: with LightDismissOverlayMode Auto (the default on a desktop) the Blue content should "
                + "stay pure Blue while the Overlay pane is open; only LightDismissOverlayMode On should dim it. "
                + "Tapping the content while the Overlay pane is open should dismiss the pane.",
            ViewKey = Views.SplitView,
        },
        new DemoScenario
        {
            Number = 6,
            Title = "Image with Stretch UniformToFill",
            Summary = "A two-colour picture in a taller box: which part of the picture the UniformToFill crop keeps.",
            DialogText =
                "An Image showing a 40 x 20 picture, Red on the left and Blue on the right, in a 200 x 300 "
                + "box with Stretch UniformToFill.\n"
                + "In the survey the box came out entirely Red: the picture was cropped from its top-left "
                + "corner, so the Blue half was never shown.",
            Instructions =
                "Compare the four boxes.\n"
                + "Look for: UniformToFill should scale the picture to 600 x 300 and keep its middle, so Red should "
                + "meet Blue down the centre of the 200 x 300 box. Uniform should letterbox the whole picture, "
                + "Fill should spread it over the box, and None should draw it at 40 x 20 in the middle. Each grey "
                + "outline is the box the Image was given; it should be exactly the size stated.",
            ViewKey = Views.ImageUniformToFill,
        },
        new DemoScenario
        {
            Number = 7,
            Title = "ToggleSwitch in its off state",
            Summary = "Does an off ToggleSwitch look the same whether it started off or was switched off?",
            DialogText =
                "Plain ToggleSwitches.\n"
                + "In the survey an off switch had two different looks: a light grey track with a dark outline, "
                + "or a pale outline with no fill (what a disabled switch uses). Which one appeared depended on "
                + "the run, not on how the switch got to off.",
            Instructions =
                "Switch A and B by tapping them, and with the three buttons, so each reaches off by different "
                + "routes.\n"
                + "Look for: every enabled switch that is off should have the same track look, and it should be "
                + "clearly different from the two disabled switches C and D. The knob should slide, and the track "
                + "should carry the accent colour only while on.",
            ViewKey = Views.ToggleSwitch,
        },
        new DemoScenario
        {
            Number = 8,
            Title = "ToggleButton after being cleared",
            Summary = "A ToggleButton tapped on and then off: does its face return to the rest look?",
            DialogText =
                "A 240 x 80 ToggleButton.\n"
                + "In the survey, after a tap to check it and a tap to clear it, the button kept a faint "
                + "hover-style fill instead of returning to its rest look. A second run did not show it.",
            Instructions =
                "Tap the ToggleButton to check it (accent fill), tap it again to clear it, and move the pointer away. "
                + "Repeat a few times; also tap the button to move keyboard focus away.\n"
                + "Look for: once cleared and no longer under the pointer, the face should match the plain Button "
                + "beside it exactly, with no lingering tint or pressed look.",
            ViewKey = Views.ToggleButton,
        },
    };

    /// <summary>The scenario with a number, or null when there is none.</summary>
    /// <param name="number">The number on the button.</param>
    /// <returns>The scenario, or null.</returns>
    public static DemoScenario Find(int number) => All.FirstOrDefault(s => s.Number == number);
}
