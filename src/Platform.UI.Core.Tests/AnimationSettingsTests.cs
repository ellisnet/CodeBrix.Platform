#nullable enable

using CodeBrix.Platform.Contracts;
using CodeBrix.Platform.Foundation.Extensibility;
using SilverAssertions;
using Windows.UI.ViewManagement;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fence for WPE1-1 item C0c: <see cref="UISettings.AnimationsEnabled"/> is the platform's system setting when the
/// platform registers <see cref="IAnimationSettingsPlatform"/>, and <see langword="true"/> (the Skia heads, which
/// register none) otherwise.
/// </summary>
public class AnimationSettingsTests
{
	private static FakeAnimationSettings? _current;
	private static bool _registered;

	[Fact]
	public void When_The_Platform_Reports_Animations_Off_Then_UISettings_Reports_Them_Off_And_Follows_Later_Changes()
	{
		//Arrange
		var settings = new FakeAnimationSettings { Enabled = false };
		Use(settings);

		try
		{
			//Act + Assert
			new UISettings().AnimationsEnabled.Should().BeFalse();
			settings.Enabled = true;
			new UISettings().AnimationsEnabled.Should().BeTrue();
		}
		finally
		{
			Use(null);
		}
	}

	[Fact]
	public void When_No_Animation_Settings_Are_Registered_Then_Animations_Are_Enabled()
	{
		//Arrange
		Use(null);

		//Act
		var enabled = new UISettings().AnimationsEnabled;

		//Assert
		enabled.Should().BeTrue();
	}

	private static void Use(FakeAnimationSettings? settings)
	{
		if (!_registered)
		{
			// A builder that returns null means "not registered" to the registry.
			ApiExtensibility.Register(typeof(IAnimationSettingsPlatform), _ => _current!);
			_registered = true;
		}

		_current = settings;
		UISettings.RefreshAnimationSettingsPlatform();
	}

	private sealed class FakeAnimationSettings : IAnimationSettingsPlatform
	{
		public bool Enabled { get; set; }

		public bool AnimationsEnabled => Enabled;
	}
}
