#nullable enable

using System.Collections.Generic;
using CodeBrix.Platform.AppSettings;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The AppSettings engine (WPE1 C13; already WinUI-free): the store, the service, the typed property handles and the
/// change notifications, driven over the in-memory storage a platform supplies - and no WinUI assembly loads.
/// </summary>
public class AppSettingsEngineTests
{
	private enum Theme
	{
		Light,
		Dark,
	}

	[Fact]
	public void When_Settings_Are_Written_Watched_And_Reopened_Then_Values_Round_Trip_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		InMemoryAppSettingsStoragePlatform.EnsureRegistered();
		AppSettingsService.Shutdown();
		AppSettingsService.Initialize("EngineTests", "/memory/engine-tests");
		var changedKeys = new List<string>();
		var volumeChanges = 0;

		try
		{
			//Act
			AppSettingsService.Store.SettingChanged += (_, e) => changedKeys.Add(e.Key);
			AppSettingsService.AddSettingHandler("Volume", (_, _) => volumeChanges++);
			AppSettingsService.Set("Volume", 7);
			AppSettingsService.Set("Volume", 7); // unchanged: no notification
			var theme = AppSettingsService.Wrap("Theme", Theme.Light);
			theme.Value = Theme.Dark;
			var created = AppSettingProperty.Create("Name", "nobody");
			created.Value = "somebody";
			AppSettingsService.Shutdown();
			AppSettingsService.Initialize("EngineTests", "/memory/engine-tests");

			//Assert
			AppSettingsService.Get("Volume", 0).Should().Be(7);
			AppSettingsService.Get("Theme", Theme.Light).Should().Be(Theme.Dark);
			AppSettingsService.Get("Name", string.Empty).Should().Be("somebody");
			AppSettingsService.Store.WasCreatedFresh.Should().BeFalse();
			volumeChanges.Should().Be(1);
			changedKeys.Should().Equal("Volume", "Theme", "Name");
		}
		finally
		{
			AppSettingsService.Shutdown();
		}

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.AppSettings.Core"); // the check sees the engine's own load
		EngineIsolation.AssertNoWinUILoaded("AppSettings");
	}

	[Fact]
	public void When_A_Store_Is_Used_Directly_Then_Missing_And_Unreadable_Values_Fall_Back_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		InMemoryAppSettingsStoragePlatform.EnsureRegistered();
		using var store = new AppSettingsStore("EngineStore", "/memory/engine-store");

		//Act
		store.Set("Count", "not a number");
		var count = store.Get("Count", 3);
		var missing = store.Get("Missing", 42);
		var removed = store.Set("Count", null);

		//Assert
		count.Should().Be(3);
		missing.Should().Be(42);
		removed.Should().BeTrue();
		store.HasValue("Count").Should().BeFalse();
		EngineIsolation.AssertNoWinUILoaded("AppSettings");
	}
}
