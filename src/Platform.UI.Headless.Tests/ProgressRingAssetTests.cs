#nullable enable

using System;
using System.IO;
using System.Reflection;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Headless.Tests;

/// <summary>
/// WPE1-11, decision D3: the ProgressRing animations are addressed in the Core assembly by default, and the Skia assembly
/// keeps its pre-split copies for one release, so an application that set the old
/// <c>embedded://CodeBrix.Platform.UI/...</c> URIs explicitly still finds the same animation. Resolved the way the
/// Lottie add-in resolves an embedded:// URI (the host is the assembly, the path the manifest resource name).
/// </summary>
public class ProgressRingAssetTests
{
	private const string ResourcePrefix = "CodeBrix.Platform.UI.UI.Xaml.Controls.ProgressRing.";

	[Theory]
	[InlineData("ProgressRingIntdeterminate.json")]
	[InlineData("ProgressRingDeterminate.json")]
	public void When_An_Application_Uses_The_PreSplit_Uri_Then_The_Skia_Assembly_Still_Has_The_Same_Animation(string file)
	{
		//Arrange
		var preSplit = new Uri($"embedded://CodeBrix.Platform.UI/{ResourcePrefix}{file}");
		var current = new Uri($"embedded://CodeBrix.Platform.UI.Core/{ResourcePrefix}{file}");

		//Act
		var preSplitJson = ReadEmbedded(preSplit);
		var currentJson = ReadEmbedded(current);

		//Assert
		preSplitJson.Should().StartWith("{");
		preSplitJson.Should().Be(currentJson);
	}

	[Fact]
	public void When_No_Application_Sets_Them_Then_The_Default_Uris_Name_The_Core_Assembly()
	{
		//Act
		var indeterminate = FeatureConfiguration.ProgressRing.ProgressRingAsset;
		var determinate = FeatureConfiguration.ProgressRing.DeterminateProgressRingAsset;

		//Assert
		indeterminate.Should().Be(new Uri($"embedded://CodeBrix.Platform.UI.Core/{ResourcePrefix}ProgressRingIntdeterminate.json"));
		determinate.Should().Be(new Uri($"embedded://CodeBrix.Platform.UI.Core/{ResourcePrefix}ProgressRingDeterminate.json"));
		ReadEmbedded(indeterminate).Should().StartWith("{");
		ReadEmbedded(determinate).Should().StartWith("{");
	}

	/// <summary>The embedded:// resolution of LottieVisualSourceBase.TryLoadEmbeddedJson.</summary>
	private static string ReadEmbedded(Uri uri)
	{
		var assembly = Assembly.Load(uri.Host);
		var resourceName = uri.AbsolutePath.Substring(1);
		using var stream = assembly.GetManifestResourceStream(resourceName);
		stream.Should().NotBeNull($"{assembly.GetName().Name} embeds {resourceName}");
		using var reader = new StreamReader(stream!);
		return reader.ReadToEnd();
	}
}
