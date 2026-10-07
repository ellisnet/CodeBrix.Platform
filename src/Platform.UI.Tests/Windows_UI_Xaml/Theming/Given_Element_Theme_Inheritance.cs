using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeBrix.Platform.UI.Tests.Helpers;
using Colors = Microsoft.UI.Colors;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_Xaml.FrameworkElementTests
{
	/// <summary>
	/// FrameworkElement.RequestedTheme set on an element re-themes the element and its subtree:
	/// ActualTheme is inherited, {ThemeResource} references resolve against the subtree's theme,
	/// and the default text foreground is the subtree theme's.
	/// </summary>
	[TestClass]
	public class Given_Element_Theme_Inheritance
	{
		private const string ProbeBrushKey = "ElementThemeProbeBrush";

		private UnitTestsApp.App _app;
		private IDisposable _appTheme;

		[TestInitialize]
		public void Initialize()
		{
			_app = UnitTestsApp.App.EnsureApplication();
			_appTheme = ThemeHelper.SetExplicitRequestedTheme(ApplicationTheme.Light);
		}

		[TestCleanup]
		public void Cleanup()
		{
			_app.HostView.Children.Clear();
			_appTheme?.Dispose();
		}

		[TestMethod]
		public void When_Ancestor_Has_RequestedTheme_Descendants_Inherit_ActualTheme()
		{
			var probe = new Border();
			var themed = new Border { RequestedTheme = ElementTheme.Dark, Child = new StackPanel { Children = { probe } } };
			var outside = new Border();
			Host(themed, outside);

			Assert.AreEqual(ElementTheme.Dark, themed.ActualTheme);
			Assert.AreEqual(ElementTheme.Dark, probe.ActualTheme);
			Assert.AreEqual(ElementTheme.Default, probe.RequestedTheme);
			Assert.AreEqual(ElementTheme.Light, outside.ActualTheme);
		}

		[TestMethod]
		public void When_Ancestor_Has_RequestedTheme_ThemeResource_Resolves_For_Subtree()
		{
			var inside = Probe();
			var outside = Probe();
			var themed = new Border { RequestedTheme = ElementTheme.Dark, Child = inside };
			Host(themed, outside);

			AssertEx.AssertHasColor(inside.Background, Colors.DarkBlue);
			AssertEx.AssertHasColor(outside.Background, Colors.LightBlue);
		}

		[TestMethod]
		public void When_RequestedTheme_Changes_At_Run_Time_Subtree_Is_Re_Resolved()
		{
			var inside = Probe();
			var themed = new Border { Child = new Grid { Children = { inside } } };
			Host(themed);
			AssertEx.AssertHasColor(inside.Background, Colors.LightBlue);

			var changes = 0;
			inside.ActualThemeChanged += (s, e) => changes++;

			themed.RequestedTheme = ElementTheme.Dark;
			AssertEx.AssertHasColor(inside.Background, Colors.DarkBlue);
			Assert.AreEqual(ElementTheme.Dark, inside.ActualTheme);
			Assert.AreEqual(1, changes);

			themed.RequestedTheme = ElementTheme.Light;
			AssertEx.AssertHasColor(inside.Background, Colors.LightBlue);
			Assert.AreEqual(2, changes);

			themed.RequestedTheme = ElementTheme.Default;
			AssertEx.AssertHasColor(inside.Background, Colors.LightBlue);
			Assert.AreEqual(ElementTheme.Light, inside.ActualTheme);
			Assert.AreEqual(2, changes, "Light -> Default in a Light app does not change the subtree's theme.");
		}

		[TestMethod]
		public void When_Element_Added_Under_Themed_Ancestor_It_Resolves_For_That_Theme()
		{
			var panel = new StackPanel();
			var themed = new Border { RequestedTheme = ElementTheme.Dark, Child = panel };
			Host(themed);

			var late = Probe();
			panel.Children.Add(late);

			AssertEx.AssertHasColor(late.Background, Colors.DarkBlue);
			Assert.AreEqual(ElementTheme.Dark, late.ActualTheme);
		}

		[TestMethod]
		public void When_Nested_Overrides_Each_Level_Resolves_Its_Own_Theme()
		{
			var innerProbe = Probe();
			var middleProbe = Probe();
			var outerProbe = Probe();
			var inner = new Border { RequestedTheme = ElementTheme.Dark, Child = innerProbe };
			var middle = new Border { RequestedTheme = ElementTheme.Light, Child = new StackPanel { Children = { middleProbe, inner } } };
			var outer = new Border { RequestedTheme = ElementTheme.Dark, Child = new StackPanel { Children = { outerProbe, middle } } };
			Host(outer);

			AssertEx.AssertHasColor(outerProbe.Background, Colors.DarkBlue);
			AssertEx.AssertHasColor(middleProbe.Background, Colors.LightBlue);
			AssertEx.AssertHasColor(innerProbe.Background, Colors.DarkBlue);
			Assert.AreEqual(ElementTheme.Light, middleProbe.ActualTheme);
			Assert.AreEqual(ElementTheme.Dark, innerProbe.ActualTheme);

			var middleChanges = 0;
			middleProbe.ActualThemeChanged += (s, e) => middleChanges++;
			outer.RequestedTheme = ElementTheme.Light;

			AssertEx.AssertHasColor(outerProbe.Background, Colors.LightBlue);
			AssertEx.AssertHasColor(middleProbe.Background, Colors.LightBlue);
			AssertEx.AssertHasColor(innerProbe.Background, Colors.DarkBlue);
			Assert.AreEqual(0, middleChanges, "A nested override shields its subtree from an outer change.");
		}

		[TestMethod]
		public void When_App_Theme_Changes_Element_Override_Is_Kept()
		{
			var inside = Probe();
			var outside = Probe();
			var insideText = new TextBlock { Text = "inside" };
			var themed = new Border { RequestedTheme = ElementTheme.Light, Child = new StackPanel { Children = { inside, insideText } } };
			Host(themed, outside);

			var insideChanges = 0;
			inside.ActualThemeChanged += (s, e) => insideChanges++;

			using (ThemeHelper.SetExplicitRequestedTheme(ApplicationTheme.Dark))
			{
				AssertEx.AssertHasColor(outside.Background, Colors.DarkBlue);
				Assert.AreEqual(ElementTheme.Dark, outside.ActualTheme);

				AssertEx.AssertHasColor(inside.Background, Colors.LightBlue);
				Assert.AreEqual(ElementTheme.Light, inside.ActualTheme);
				Assert.IsTrue(Luminance(insideText.Foreground) < 0.2, "Default text under a Light override stays dark in a Dark app.");
				Assert.AreEqual(0, insideChanges);
			}
		}

		[TestMethod]
		public void When_Root_RequestedTheme_Sets_App_Theme_Descendants_Are_Notified()
		{
			var probe = Probe();
			var shielded = Probe();
			Host(probe, new Border { RequestedTheme = ElementTheme.Light, Child = shielded });

			var probeChanges = 0;
			var shieldedChanges = 0;
			probe.ActualThemeChanged += (s, e) => probeChanges++;
			shielded.ActualThemeChanged += (s, e) => shieldedChanges++;

			var root = (FrameworkElement)_app.HostView;
			try
			{
				root.RequestedTheme = ElementTheme.Dark;

				Assert.AreEqual(ApplicationTheme.Dark, Application.Current.RequestedTheme);
				Assert.AreEqual(ElementTheme.Dark, probe.ActualTheme);
				AssertEx.AssertHasColor(probe.Background, Colors.DarkBlue);
				Assert.AreEqual(1, probeChanges);

				Assert.AreEqual(ElementTheme.Light, shielded.ActualTheme);
				AssertEx.AssertHasColor(shielded.Background, Colors.LightBlue);
				Assert.AreEqual(0, shieldedChanges);
			}
			finally
			{
				root.RequestedTheme = ElementTheme.Default;
			}
		}

		[TestMethod]
		public void When_Default_TextBlock_Foreground_Follows_Subtree_Theme()
		{
			var inside = new TextBlock { Text = "inside" };
			var outside = new TextBlock { Text = "outside" };
			var themed = new Border { RequestedTheme = ElementTheme.Dark, Child = inside };
			Host(themed, outside);

			Assert.IsTrue(Luminance(inside.Foreground) > 0.8, $"Expected light text under a Dark subtree, got {Describe(inside.Foreground)}.");
			Assert.IsTrue(Luminance(outside.Foreground) < 0.2, $"Expected dark text in a Light app, got {Describe(outside.Foreground)}.");

			themed.RequestedTheme = ElementTheme.Default;
			Assert.IsTrue(Luminance(inside.Foreground) < 0.2, $"Expected dark text after clearing the override, got {Describe(inside.Foreground)}.");

			themed.RequestedTheme = ElementTheme.Dark;
			Assert.IsTrue(Luminance(inside.Foreground) > 0.8, $"Expected light text after setting the override again, got {Describe(inside.Foreground)}.");
		}

		[TestMethod]
		public void When_Ancestor_Passes_Down_A_Foreground_Themed_Subtree_Restarts_From_Its_Theme_Default()
		{
			var inside = new TextBlock { Text = "inside" };
			var outside = new TextBlock { Text = "outside" };
			var themed = new Border { RequestedTheme = ElementTheme.Dark, Child = new StackPanel { Children = { inside } } };
			var page = new UserControl
			{
				Foreground = new SolidColorBrush(Colors.Black),
				Content = new StackPanel { Children = { themed, outside } },
			};
			Host(page);

			AssertEx.AssertHasColor(outside.Foreground, Colors.Black);
			Assert.IsTrue(Luminance(inside.Foreground) > 0.8, $"Expected the Dark theme's default text, got {Describe(inside.Foreground)}.");

			var late = new TextBlock { Text = "late" };
			((StackPanel)themed.Child).Children.Add(late);
			Assert.IsTrue(Luminance(late.Foreground) > 0.8, $"A TextBlock added later must not pick up the outer Foreground, got {Describe(late.Foreground)}.");

			themed.RequestedTheme = ElementTheme.Default;
			AssertEx.AssertHasColor(inside.Foreground, Colors.Black);
		}

		[TestMethod]
		public void When_Explicit_Foreground_Under_Themed_Ancestor_It_Is_Kept()
		{
			var text = new TextBlock { Text = "explicit", Foreground = new SolidColorBrush(Colors.Peru) };
			var themed = new Border { RequestedTheme = ElementTheme.Dark, Child = text };
			Host(themed);

			AssertEx.AssertHasColor(text.Foreground, Colors.Peru);
		}

		private static Border Probe()
		{
			var probe = new Border { Width = 10, Height = 10 };
			ResourceResolver.ApplyResource(probe, Border.BackgroundProperty, ProbeBrushKey, isThemeResourceExtension: true, isHotReloadSupported: false);
			return probe;
		}

		private void Host(params UIElement[] children)
		{
			var root = new StackPanel { Resources = ThemedBrushes() };
			foreach (var child in children)
			{
				root.Children.Add(child);
			}

			_app.HostView.Children.Add(root);
		}

		private static ResourceDictionary ThemedBrushes()
		{
			var resources = new ResourceDictionary();
			resources.ThemeDictionaries["Light"] = new ResourceDictionary { [ProbeBrushKey] = new SolidColorBrush(Colors.LightBlue) };
			resources.ThemeDictionaries["Dark"] = new ResourceDictionary { [ProbeBrushKey] = new SolidColorBrush(Colors.DarkBlue) };
			return resources;
		}

		private static double Luminance(Brush brush)
		{
			var color = ((SolidColorBrush)brush).Color;
			return (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;
		}

		private static string Describe(Brush brush) => brush is SolidColorBrush solid ? solid.Color.ToString() : brush?.GetType().Name ?? "null";
	}
}
