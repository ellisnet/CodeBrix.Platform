using System.Text.Json.Serialization;

namespace CodeBrix.Platform.PackageDependencyValidator.Models;

/// <summary>
/// The contents of build/nuget/package-dependency-map.json: which .nuspec packs which
/// project(s), plus the deliberate exceptions.
/// </summary>
public sealed class PackageMap
{
	/// <summary>Package ids that are never inspected at all.</summary>
	[JsonPropertyName("excludedPackages")]
	public List<string> ExcludedPackages { get; set; } = new();

	/// <summary>
	/// Package ids whose version is a hard family-wide lock (SkiaSharp / HarfBuzzSharp).
	/// Every packed project must resolve the SAME version of these, and every nuspec that
	/// declares one must declare that version.
	/// </summary>
	[JsonPropertyName("lockedPackages")]
	public List<string> LockedPackages { get; set; } = new();

	/// <summary>The nuspec-driven packages that are checked.</summary>
	[JsonPropertyName("packages")]
	public List<MappedPackage> Packages { get; set; } = new();

	/// <summary>
	/// Where the consumer-side half of the hard lock declares its required versions, so the gate
	/// can prove that literal always matches what the packed projects actually resolve.
	/// </summary>
	[JsonPropertyName("lockDeclaration")]
	public LockDeclaration? LockDeclaration { get; set; }

	/// <summary>
	/// Repo-relative project path -&gt; the package that project ships in. Lets the gate see
	/// dependencies that arrive through a ProjectReference to a SIBLING package rather than
	/// through a PackageReference.
	/// </summary>
	[JsonPropertyName("projectPackages")]
	public Dictionary<string, string> ProjectPackages { get; set; } = new();
}

/// <summary>
/// Points at the MSBuild file shipped in buildTransitive that tells consuming applications which
/// locked versions CodeBrix.Platform requires (see UNOB0002 / UNOB0003).
/// </summary>
public sealed class LockDeclaration
{
	/// <summary>File name, relative to build/nuget.</summary>
	[JsonPropertyName("file")]
	public string File { get; set; } = "";

	/// <summary>Locked package id -&gt; the MSBuild property that declares its required version.</summary>
	[JsonPropertyName("properties")]
	public Dictionary<string, string> Properties { get; set; } = new();
}

/// <summary>One nuspec-driven package and the projects whose output it packs.</summary>
public sealed class MappedPackage
{
	/// <summary>The produced NuGet package id.</summary>
	[JsonPropertyName("packageId")]
	public string PackageId { get; set; } = "";

	/// <summary>File name of the .nuspec, relative to build/nuget.</summary>
	[JsonPropertyName("nuspec")]
	public string Nuspec { get; set; } = "";

	/// <summary>Repo-relative paths of the project(s) this package packs.</summary>
	[JsonPropertyName("projects")]
	public List<string> Projects { get; set; } = new();

	/// <summary>
	/// Package ids the nuspec declares on purpose even though no packed project has a matching
	/// PackageReference — e.g. dependencies that arrive through a ProjectReference to a sibling
	/// package. Each entry must carry a reason in the map file's comment block.
	/// </summary>
	[JsonPropertyName("allowedExtraDependencies")]
	public List<string> AllowedExtraDependencies { get; set; } = new();

	/// <summary>
	/// Package ids a packed project references but the nuspec deliberately does NOT declare —
	/// e.g. packages guaranteed to arrive via the platform head package.
	/// </summary>
	[JsonPropertyName("allowedMissingDependencies")]
	public List<string> AllowedMissingDependencies { get; set; } = new();
}
