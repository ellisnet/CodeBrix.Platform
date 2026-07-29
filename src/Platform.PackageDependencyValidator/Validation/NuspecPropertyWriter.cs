using System.Text;

using CodeBrix.Platform.PackageDependencyValidator.Models;

namespace CodeBrix.Platform.PackageDependencyValidator.Validation;

/// <summary>
/// Writes the pack-time tokens that let a .nuspec state a dependency version WITHOUT authoring it.
/// </summary>
/// <remarks>
/// This is the half of the design that removes the second place to author a version: instead of a
/// literal, the nuspec carries <c>version="$dep_Some_Package_Id$"</c>, and the value comes straight
/// from the packed project's own PackageReference. The .csproj — the view Visual Studio's Solution
/// NuGet package view manages — is then the only place a version exists.
///
/// Locked package ids (SkiaSharp / HarfBuzzSharp) are deliberately NOT emitted: they stay literal
/// in the nuspec and are governed by the family hard lock instead, so that moving them is always a
/// deliberate, visible edit rather than something that follows a csproj automatically.
/// </remarks>
public static class NuspecPropertyWriter
{
	/// <summary>
	/// Writes <c>&lt;nuspec file name&gt;.props</c> into <paramref name="outputDirectory"/>, defining
	/// <c>CbxExtraNuspecProperties</c> for the pack shim to append to its NuspecProperties.
	/// </summary>
	public static void Write(
		string outputDirectory,
		MappedPackage package,
		IReadOnlyDictionary<string, string> referenced,
		IReadOnlyCollection<string> lockedPackages)
	{
		Directory.CreateDirectory(outputDirectory);

		var tokens = referenced
			.Where(r => !lockedPackages.Contains(r.Key, StringComparer.OrdinalIgnoreCase))
			.OrderBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
			.Select(r => $"{ToToken(r.Key)}={r.Value}");

		var builder = new StringBuilder();
		builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
		builder.AppendLine("<!-- GENERATED at pack time by CodeBrix.Platform.PackageDependencyValidator. Do not edit. -->");
		builder.AppendLine("<Project>");
		builder.AppendLine("\t<PropertyGroup>");
		builder.Append("\t\t<CbxExtraNuspecProperties>");
		builder.Append(string.Join(';', tokens));
		builder.AppendLine("</CbxExtraNuspecProperties>");
		builder.AppendLine("\t</PropertyGroup>");
		builder.AppendLine("</Project>");

		var fileName = $"{Path.GetFileNameWithoutExtension(package.Nuspec)}.props";

		File.WriteAllText(Path.Combine(outputDirectory, fileName), builder.ToString());
	}

	/// <summary>
	/// Turns a package id into a nuspec token name: <c>CodeBrix.Platform.OpenGL.MitLicenseForever</c>
	/// becomes <c>dep_CodeBrix_Platform_OpenGL_MitLicenseForever</c>, used as
	/// <c>$dep_CodeBrix_Platform_OpenGL_MitLicenseForever$</c>.
	/// </summary>
	public static string ToToken(string packageId)
		=> "dep_" + packageId.Replace('.', '_').Replace('-', '_');
}
