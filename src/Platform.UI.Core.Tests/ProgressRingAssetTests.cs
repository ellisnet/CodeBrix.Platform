#nullable enable

using System;
using System.IO;
using System.Reflection;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// WPE1-11, decision D3: the default <see cref="FeatureConfiguration.ProgressRing"/> animation URIs name the Core
/// assembly, which embeds the two animations, so a ProgressRing finds them where only the Core assemblies are loaded
/// (CodeBrix.Android, CodeBrix.Mobile). The URIs are resolved the way the Lottie add-in resolves an embedded:// URI
/// (the host is the assembly, the path the manifest resource name).
/// </summary>
public class ProgressRingAssetTests
{
	[Fact]
	public void When_No_Application_Sets_Them_Then_The_ProgressRing_Assets_Are_Embedded_In_The_Core_Assembly()
	{
		//Arrange
		var indeterminate = FeatureConfiguration.ProgressRing.ProgressRingAsset;
		var determinate = FeatureConfiguration.ProgressRing.DeterminateProgressRingAsset;

		//Act
		var indeterminateJson = ReadEmbedded(indeterminate);
		var determinateJson = ReadEmbedded(determinate);

		//Assert
		indeterminate.Scheme.Should().Be("embedded");
		indeterminate.Host.Should().Be("codebrix.platform.ui.core");
		determinate.Host.Should().Be("codebrix.platform.ui.core");
		indeterminateJson.Should().StartWith("{");
		determinateJson.Should().StartWith("{");
		indeterminateJson.Should().NotBe(determinateJson);
	}

	/// <summary>The embedded:// resolution of LottieVisualSourceBase.TryLoadEmbeddedJson.</summary>
	private static string ReadEmbedded(Uri uri)
	{
		var assembly = Assembly.Load(uri.Host);
		var resourceName = uri.AbsolutePath.Substring(1);
		using var stream = assembly.GetManifestResourceStream(resourceName);
		stream.Should().NotBeNull($"{assembly.GetName().Name} embeds {resourceName}");
		using var reader = new StreamReader(stream!);
		return reader.ReadToEnd().TrimStart();
	}
}
