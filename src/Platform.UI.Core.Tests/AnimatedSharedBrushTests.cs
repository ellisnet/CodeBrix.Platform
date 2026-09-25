#nullable enable

using CodeBrix.Platform.UI.DataBinding;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SilverAssertions;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fence for WPE1-1 item C0g: an animation whose target path runs through a brush (a template storyboard's
/// "(Shape.Fill).(SolidColorBrush.Color)") animates a copy of that brush, never the brush itself - the brush usually is
/// a theme resource shared by every control (the ProgressBar's Paused/Error states turned AccentFillColorDefaultBrush
/// brown for the whole application). The XAML generator writes such paths namespace-qualified, the form the
/// clone step did not recognize. The pixel-level fence is the UIReqs scenario "A paused ProgressBar that goes away leaves the accent colour
/// as it was" (Range/ProgressBar).
/// </summary>
public class AnimatedSharedBrushTests
{
	/// <summary>The target path exactly as the XAML generator writes it for the ProgressBar template's ColorAnimation.</summary>
	private const string TemplateTargetPath = "(Microsoft.UI.Xaml.Shapes:Shape.Fill).(Microsoft.UI.Xaml.Media:SolidColorBrush.Color)";

	[Fact]
	public void When_An_Animation_Targets_The_Color_Of_A_Shared_Fill_Then_A_Copy_Of_The_Brush_Is_Animated()
	{
		//Arrange
		var shared = new SolidColorBrush(Colors.Blue);
		var rectangle = new Rectangle { Fill = shared };
		var other = new Rectangle { Fill = shared };
		using var path = new BindingPath(TemplateTargetPath, null, forAnimations: true, allowPrivateMembers: false) { DataContext = rectangle };
		var caution = Color.FromArgb(0xFF, 0x9D, 0x5D, 0x00);

		//Act: what a timeline does when it begins, then one animated value
		path.CloneShareableObjectsInPath();
		path.Value = caution;

		//Assert
		shared.Color.Should().Be(Colors.Blue);
		other.Fill.Should().BeSameAs(shared);
		rectangle.Fill.Should().NotBeSameAs(shared);
		((SolidColorBrush)rectangle.Fill).Color.Should().Be(caution);
	}
}
