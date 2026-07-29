using System.IO.Compression;
using System.Xml.Linq;

namespace CodeBrix.Platform.PackageDependencyValidator.Validation;

/// <summary>Reads the declared &lt;dependencies&gt; out of a .nuspec file or a packed .nupkg.</summary>
public static class NuspecReader
{
	/// <summary>Reads dependency id -&gt; version from a .nuspec on disk (pre-pack).</summary>
	public static Dictionary<string, string> ReadFromNuspec(string nuspecPath)
		=> ReadDependencies(XDocument.Load(nuspecPath));

	/// <summary>Reads dependency id -&gt; version from the .nuspec inside a produced .nupkg (post-pack).</summary>
	public static Dictionary<string, string> ReadFromPackage(string nupkgPath)
	{
		using var archive = ZipFile.OpenRead(nupkgPath);

		var entry = archive.Entries.FirstOrDefault(e =>
			e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)
			&& !e.FullName.Contains('/'))
			?? throw new InvalidOperationException($"No .nuspec found in '{nupkgPath}'.");

		using var stream = entry.Open();

		return ReadDependencies(XDocument.Load(stream));
	}

	/// <summary>Reads the package id declared by a .nuspec on disk.</summary>
	public static string ReadPackageId(string nuspecPath)
	{
		var id = XDocument.Load(nuspecPath)
			.Descendants()
			.FirstOrDefault(e => e.Name.LocalName == "id")?
			.Value;

		return id?.Trim() ?? "";
	}

	/// <summary>
	/// True when a nuspec version value is an unsubstituted pack-time token such as
	/// <c>$codebrixversion$</c>. Tokens carry no version to compare, so callers skip them.
	/// </summary>
	public static bool IsToken(string version)
		=> version.Length > 1 && version.StartsWith('$') && version.EndsWith('$');

	private static Dictionary<string, string> ReadDependencies(XDocument document)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		var dependencies = document
			.Descendants()
			.Where(e => e.Name.LocalName == "dependency");

		foreach (var dependency in dependencies)
		{
			var id = dependency.Attribute("id")?.Value;

			if (string.IsNullOrWhiteSpace(id))
			{
				continue;
			}

			result[id.Trim()] = dependency.Attribute("version")?.Value.Trim() ?? "";
		}

		return result;
	}
}
