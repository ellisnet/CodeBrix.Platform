using CodeBrix.Platform.PackageDependencyValidator.Models;

namespace CodeBrix.Platform.PackageDependencyValidator.Validation;

/// <summary>
/// Compares what a .nuspec DECLARES against what the project(s) it packs actually REFERENCE.
/// </summary>
/// <remarks>
/// Declaration-vs-declaration only: no assemblies are read, no package feed is contacted, and no
/// opinion is formed about whether a version is the newest available. Choosing versions is the
/// maintainer's job, done in Visual Studio; this gate only guarantees that nothing downstream
/// silently states a DIFFERENT version than the one that was chosen.
/// </remarks>
public sealed class DependencyValidator
{
	private readonly PackageMap _map;

	/// <summary>Creates a validator over the supplied package map.</summary>
	public DependencyValidator(PackageMap map) => _map = map;

	/// <summary>
	/// Validates one package. <paramref name="declared"/> is the nuspec's dependency set,
	/// <paramref name="referenced"/> the union of the packed projects' PackageReferences.
	/// </summary>
	public IReadOnlyList<Finding> Validate(
		MappedPackage package,
		IReadOnlyDictionary<string, string> declared,
		IReadOnlyDictionary<string, string> referenced,
		IReadOnlyCollection<string> requiredFromProjectReferences)
	{
		var findings = new List<Finding>();

		foreach (var (id, declaredVersion) in declared)
		{
			if (!referenced.TryGetValue(id, out var referencedVersion))
			{
				if (!package.AllowedExtraDependencies.Contains(id, StringComparer.OrdinalIgnoreCase)
					&& !requiredFromProjectReferences.Contains(id, StringComparer.OrdinalIgnoreCase))
				{
					findings.Add(new Finding(
						FindingSeverity.Warning,
						package.PackageId,
						id,
						$"nuspec declares a dependency on '{id}' ({Describe(declaredVersion)}), but no project "
						+ "packed by this package has a PackageReference to it. Either it is stale and should be "
						+ "removed, or it should be listed in allowedExtraDependencies with a reason."));
				}

				continue;
			}

			if (NuspecReader.IsToken(declaredVersion))
			{
				// A pack-time token is substituted by the driver from this same PackageReference
				// value, so there is nothing to disagree about.
				continue;
			}

			if (!string.Equals(declaredVersion, referencedVersion, StringComparison.OrdinalIgnoreCase))
			{
				findings.Add(new Finding(
					FindingSeverity.Error,
					package.PackageId,
					id,
					$"VERSION MISMATCH on '{id}': the nuspec declares {declaredVersion} but the packed "
					+ $"project(s) reference {referencedVersion}. The .csproj is authoritative — the nuspec "
					+ "must not state a different version."));
			}
		}

		foreach (var (id, referencedVersion) in referenced)
		{
			if (declared.ContainsKey(id)
				|| package.AllowedMissingDependencies.Contains(id, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			findings.Add(new Finding(
				FindingSeverity.Error,
				package.PackageId,
				id,
				$"MISSING DEPENDENCY '{id}' {referencedVersion}: a project packed by this package "
				+ "references it, but the nuspec does not declare it, so consumers will not receive it. "
				+ "Add the dependency, or list it in allowedMissingDependencies with a reason."));
		}

		return findings;
	}

	/// <summary>
	/// Verifies dependencies that arrive through a ProjectReference to a sibling package.
	/// </summary>
	/// <remarks>
	/// Only EXISTENCE is checked, never the version: these are family packages whose version is
	/// stamped at pack time from the shared $codebrixversion$ token, so there is no independently
	/// authored version that could disagree.
	/// </remarks>
	public IReadOnlyList<Finding> ValidateProjectReferenceDependencies(
		MappedPackage package,
		IReadOnlyDictionary<string, string> declared,
		IReadOnlyCollection<string> requiredFromProjectReferences)
	{
		var findings = new List<Finding>();

		foreach (var id in requiredFromProjectReferences)
		{
			if (declared.ContainsKey(id)
				|| package.AllowedMissingDependencies.Contains(id, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			findings.Add(new Finding(
				FindingSeverity.Error,
				package.PackageId,
				id,
				$"MISSING DEPENDENCY '{id}': a project packed by this package has a ProjectReference to "
				+ "a project that ships in that package, so the assemblies here bind against it, but the "
				+ "nuspec does not declare it. Consumers who do not happen to reference it some other way "
				+ "will fail at runtime. Add the dependency, or list it in allowedMissingDependencies."));
		}

		return findings;
	}

	/// <summary>
	/// Verifies the family-wide SkiaSharp / HarfBuzzSharp lock: every packed project across every
	/// package must resolve the SAME version of each locked package id.
	/// </summary>
	/// <param name="observed">
	/// Locked package id -&gt; (version -&gt; the projects that resolved that version).
	/// </param>
	public IReadOnlyList<Finding> ValidateLock(
		IReadOnlyDictionary<string, Dictionary<string, List<string>>> observed)
	{
		var findings = new List<Finding>();

		foreach (var lockedId in _map.LockedPackages)
		{
			if (!observed.TryGetValue(lockedId, out var versions) || versions.Count < 2)
			{
				continue;
			}

			var detail = string.Join(
				"; ",
				versions.Select(v => $"{v.Key} <- {string.Join(", ", v.Value)}"));

			findings.Add(new Finding(
				FindingSeverity.Error,
				"(family lock)",
				lockedId,
				$"LOCK VIOLATION on '{lockedId}': the family is hard-locked to a single version, but "
				+ $"projects resolve more than one — {detail}."));
		}

		return findings;
	}

	/// <summary>
	/// Verifies the consumer-side half of the lock: the required-version literals shipped in
	/// buildTransitive (which produce UNOB0002 / UNOB0003 in consuming applications) must equal the
	/// version the packed projects actually resolve. Without this, the lock could go stale and
	/// start rejecting the very version CodeBrix.Platform ships against.
	/// </summary>
	public IReadOnlyList<Finding> ValidateLockDeclaration(
		IReadOnlyDictionary<string, string> declaredLockVersions,
		IReadOnlyDictionary<string, Dictionary<string, List<string>>> observed)
	{
		var findings = new List<Finding>();

		foreach (var (packageId, declaredVersion) in declaredLockVersions)
		{
			if (!observed.TryGetValue(packageId, out var versions) || versions.Count != 1)
			{
				continue;
			}

			var resolvedVersion = versions.Keys.First();

			if (!string.Equals(declaredVersion, resolvedVersion, StringComparison.OrdinalIgnoreCase))
			{
				findings.Add(new Finding(
					FindingSeverity.Error,
					"(consumer lock)",
					packageId,
					$"LOCK DECLARATION STALE for '{packageId}': the buildTransitive lock tells consuming "
					+ $"applications to use {declaredVersion}, but the packed projects are built against "
					+ $"{resolvedVersion}. Update the required-version literal so the lock matches what "
					+ "CodeBrix.Platform actually ships against."));
			}
		}

		return findings;
	}

	private static string Describe(string version)
		=> string.IsNullOrWhiteSpace(version) ? "no version" : version;
}
