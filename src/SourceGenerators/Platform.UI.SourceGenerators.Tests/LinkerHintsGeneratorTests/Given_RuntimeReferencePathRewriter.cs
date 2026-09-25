using System.IO;
using CodeBrix.Platform.UI.Tasks.LinkerHintsGenerator;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.LinkerHintsGeneratorTests;

/// <summary>
/// Fences the linker-hint generator's mapping of a framework package reference to its runtime copy: the runtime
/// folder is named after the build's target framework (net10.0), and the platform-neutral Core assemblies, which
/// ship in lib/ only, are never mapped.
/// </summary>
[TestClass]
public class Given_RuntimeReferencePathRewriter
{
	private static readonly char S = Path.DirectorySeparatorChar;
	private static readonly string PackageBase = $"{S}packages";
	private static readonly string LibFolder = $"{PackageBase}{S}codebrix.platform.apachelicenseforever{S}1.0.0{S}lib{S}net10.0";
	private static readonly string RuntimeFolder = $"{PackageBase}{S}codebrix.platform.apachelicenseforever{S}1.0.0{S}codebrix-platform-runtime{S}net10.0{S}skia";

	[TestMethod]
	public void When_Skia_Assembly_Has_A_Runtime_Copy_Then_The_Runtime_Copy_Is_Used()
	{
		var reference = $"{LibFolder}{S}CodeBrix.Platform.UI.dll";
		var expected = $"{RuntimeFolder}{S}CodeBrix.Platform.UI.dll";

		var result = RuntimeReferencePathRewriter.Rewrite(reference, PackageBase, "Skia", "10.0", path => path == expected);

		result.Should().Be(expected);
	}

	[TestMethod]
	public void When_Core_Assembly_Then_The_Lib_Path_Is_Kept()
	{
		var reference = $"{LibFolder}{S}CodeBrix.Platform.UI.Core.dll";

		var result = RuntimeReferencePathRewriter.Rewrite(reference, PackageBase, "Skia", "10.0", _ => true);

		result.Should().Be(reference);
	}

	[TestMethod]
	public void When_No_Runtime_Copy_Exists_Then_The_Lib_Path_Is_Kept()
	{
		var reference = $"{LibFolder}{S}CodeBrix.Platform.UI.dll";

		var result = RuntimeReferencePathRewriter.Rewrite(reference, PackageBase, "Skia", "10.0", _ => false);

		result.Should().Be(reference);
	}

	[TestMethod]
	public void When_Not_A_Runtime_Build_Then_The_Lib_Path_Is_Kept()
	{
		var reference = $"{LibFolder}{S}CodeBrix.Platform.UI.dll";

		var result = RuntimeReferencePathRewriter.Rewrite(reference, PackageBase, "Core", "10.0", _ => true);

		result.Should().Be(reference);
	}
}
