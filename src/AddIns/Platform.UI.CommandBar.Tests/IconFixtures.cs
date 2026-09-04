using System;
using System.IO;
using System.Reflection;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The five icon fixtures the suite measures against, reachable three ways.
/// </summary>
/// <remarks>
/// Each file under <c>Fixtures/</c> is both EMBEDDED in this assembly - so the add-in's
/// <c>cb-res://</c> scheme has something real to resolve - and COPIED beside the test binary, so
/// the same bytes can be reached through a <c>file:</c> URI. The two views of one file are what let
/// a test compare what the scheme found with what is on disk.
/// </remarks>
internal static class IconFixtures
{
	/// <summary>An SVG that paints with <c>currentColor</c>: a 24x24 square, no colour of its own.</summary>
	internal const string CurrentColorSvg = "currentcolor.svg";

	/// <summary>An SVG with hard-coded black fill and stroke: a 24x24 square that states its colour.</summary>
	internal const string MonochromeSvg = "monochrome.svg";

	/// <summary>An 8x8 PNG: the left half opaque #00AA00, the right half fully transparent.</summary>
	internal const string AlphaPng = "alpha.png";

	/// <summary>An 8x8 JPEG, solid #0000FF - opaque, as JPEG always is.</summary>
	internal const string Jpeg = "photo.jpg";

	/// <summary>An 8x8 24-bit BMP, solid #00AA00.</summary>
	internal const string Bmp = "swatch.bmp";

	/// <summary>This assembly's simple name, which is what a <c>cb-res://</c> URI names.</summary>
	internal static string AssemblyName { get; } = typeof(IconFixtures).Assembly.GetName().Name!;

	/// <summary>The directory the fixtures were copied into beside the test binary.</summary>
	internal static string Directory { get; } =
		Path.Combine(Path.GetDirectoryName(typeof(IconFixtures).Assembly.Location)!, "Fixtures");

	/// <summary>The <c>cb-res://</c> URI naming one fixture inside this assembly.</summary>
	/// <param name="fileName">One of the constants on this class.</param>
	/// <returns>An embedded-resource URI.</returns>
	internal static Uri ResourceUri(string fileName)
		=> new($"{IconResourceScheme.Scheme}://{AssemblyName}/{fileName}");

	/// <summary>The <c>file:</c> URI naming one fixture beside the test binary.</summary>
	/// <param name="fileName">One of the constants on this class.</param>
	/// <returns>A file URI.</returns>
	internal static Uri FileUri(string fileName) => new(Path.Combine(Directory, fileName));

	/// <summary>One fixture's bytes, read from this assembly.</summary>
	/// <param name="fileName">One of the constants on this class.</param>
	/// <returns>The file's bytes.</returns>
	internal static byte[] Bytes(string fileName)
	{
		using var stream = Open(fileName);
		using var buffer = new MemoryStream();
		stream.CopyTo(buffer);
		return buffer.ToArray();
	}

	/// <summary>One fixture's text, read from this assembly.</summary>
	/// <param name="fileName">One of the constants on this class.</param>
	/// <returns>The file's text.</returns>
	internal static string Text(string fileName)
	{
		using var stream = Open(fileName);
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	private static Stream Open(string fileName)
	{
		var assembly = typeof(IconFixtures).Assembly;
		var name = Array.Find(
			assembly.GetManifestResourceNames(),
			candidate => candidate.EndsWith("." + fileName, StringComparison.Ordinal))
			?? throw new InvalidOperationException(
				$"The fixture '{fileName}' is not embedded in {AssemblyName}. The test project's "
				+ "Fixtures ItemGroup needs checking.");

		return assembly.GetManifestResourceStream(name)!;
	}
}
