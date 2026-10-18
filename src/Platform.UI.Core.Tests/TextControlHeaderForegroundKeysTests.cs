#nullable enable

using CodeBrix.Platform.UI;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fences for the header-foreground key family in the framework's system resources: the PointerOver and Pressed members
/// resolve (a platform library that maps a key family's states onto a native widget probes them, and an unresolved
/// probe logs "Couldn't statically resolve resource" on every lookup) and show the same colour as the plain key, so no
/// template or widget changes its look.
/// </summary>
public class TextControlHeaderForegroundKeysTests
{
	[Theory]
	[InlineData("TextControlHeaderForegroundPointerOver")]
	[InlineData("TextControlHeaderForegroundPressed")]
	public void When_A_Header_Foreground_State_Key_Is_Resolved_Then_It_Is_The_Plain_Keys_Colour(string key)
	{
		//Act
		var plain = ResourceResolver.ResolveResourceStatic("TextControlHeaderForeground", typeof(object)) as SolidColorBrush;
		var state = ResourceResolver.ResolveResourceStatic(key, typeof(object)) as SolidColorBrush;

		//Assert
		plain.Should().NotBeNull();
		state.Should().NotBeNull($"{key} is defined beside TextControlHeaderForeground in every theme dictionary");
		state!.Color.Should().Be(plain!.Color);
	}

	[Fact]
	public void When_A_Key_Outside_The_Family_Is_Resolved_Then_Nothing_Is_Found()
	{
		//Act (the control: the lookup above does not answer every key)
		var missing = ResourceResolver.ResolveResourceStatic("TextControlHeaderForegroundFocused", typeof(object));

		//Assert
		missing.Should().BeNull();
	}
}
