using System;
using System.IO;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// WPE1-14 (FIXLIST_platform_core_split [WPE1-13] FOUND, desktop ms-appx): <see cref="Uri.Host"/> is lower-cased, so an
/// icon named ms-appx://MyLibrary/... was looked for under "mylibrary/" and missed a "MyLibrary" folder on a
/// case-sensitive file system. The host as written wins when its path exists; otherwise the lower-cased path of before.
/// </summary>
public sealed class IconAssetLocatorTests : IDisposable
{
	private readonly string _installed = Path.Combine(Path.GetTempPath(), "wpe1-14-icons-" + Guid.NewGuid().ToString("N"));

	public IconAssetLocatorTests() => Directory.CreateDirectory(_installed);

	public void Dispose() => Directory.Delete(_installed, recursive: true);

	[Fact]
	public void a_library_folder_with_the_written_case_is_found()
	{
		//Arrange
		Write("MyLibrary/Icons/open.png");

		//Act
		var host = Resolve("ms-appx://MyLibrary/Icons/open.png");

		//Assert
		host.Should().Be("MyLibrary");
	}

	[Fact]
	public void a_lower_case_library_folder_is_still_found()
	{
		//Arrange
		Write("mylibrary/Icons/open.png");

		//Act
		var host = Resolve("ms-appx://MyLibrary/Icons/open.png");

		//Assert
		host.Should().Be("mylibrary");
	}

	[Fact]
	public void when_both_library_folders_exist_the_written_case_wins()
	{
		//Arrange
		Write("MyLibrary/Icons/open.png");
		Write("mylibrary/Icons/open.png");

		//Act
		var host = Resolve("ms-appx://MyLibrary/Icons/open.png");

		//Assert
		host.Should().Be("MyLibrary");
	}

	[Fact]
	public void a_scale_variant_only_file_maps_into_the_written_case_folder_that_exists()
	{
		//Arrange
		Write("MyLibrary/Icons/open.scale-200.png");

		//Act
		var host = Resolve("ms-appx://MyLibrary/Icons/open.png");

		//Assert
		host.Should().Be("MyLibrary");
	}

	[Fact]
	public void a_uri_without_a_host_maps_to_no_folder()
	{
		//Act
		var host = Resolve("ms-appx:///Icons/open.png");

		//Assert
		host.Should().BeEmpty();
	}

	private string Resolve(string uri)
	{
		var parsed = new Uri(uri);
		return CodeBrix.Platform.UI.CommandBar.IconAssetLocator.ResolveHostFolder(parsed, _installed, Uri.UnescapeDataString(parsed.AbsolutePath).TrimStart('/'));
	}

	private void Write(string relativePath)
	{
		var full = Path.Combine(_installed, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(full)!);
		File.WriteAllBytes(full, [1, 2, 3]);
	}
}
