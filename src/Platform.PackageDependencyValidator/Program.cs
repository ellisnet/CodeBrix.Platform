using System.Text.Json;

using CodeBrix.ArgumentParser;

using CodeBrix.Platform.PackageDependencyValidator.Models;
using CodeBrix.Platform.PackageDependencyValidator.Validation;

// CodeBrix.Platform package dependency gate.
//
// Guarantees that a .nuspec can never state a dependency version that differs from the
// PackageReference version of the project(s) it packs. Version SELECTION is the maintainer's job
// (done in Visual Studio's Solution NuGet package view); this tool only enforces that every other
// file agrees with what was selected there. It never contacts a package feed and never forms an
// opinion about whether a version is up to date.

string? mapPath = null;
string? repoRoot = null;
string? nuspecDir = null;
string? packageDir = null;
string? emitPropertiesDir = null;
var targetFramework = "net10.0";
var noFail = false;

var options = new OptionSet();
options.Add("map=", "path to package-dependency-map.json", s => mapPath = s);
options.Add("repo-root=", "path to the repository root", s => repoRoot = s);
options.Add("nuspec-dir=", "directory holding the source .nuspec files", s => nuspecDir = s);
options.Add("package-dir=", "directory holding produced .nupkg files (post-pack mode)", s => packageDir = s);
options.Add("emit-properties=", "directory to write per-nuspec pack-time token .props files into", s => emitPropertiesDir = s);
options.Add("target-framework=", "target framework to evaluate (default net10.0)", s => targetFramework = s);
options.Add("no-fail", "report findings but always exit 0", _ => noFail = true);

try
{
	options.Parse(args);
}
catch (OptionException e)
{
	Console.WriteLine(e.Message);

	return -1;
}

if (mapPath is null || !File.Exists(mapPath))
{
	Console.WriteLine("A valid --map path is required.");

	return -1;
}

if (repoRoot is null || !Directory.Exists(repoRoot))
{
	Console.WriteLine("A valid --repo-root path is required.");

	return -1;
}

nuspecDir ??= Path.GetDirectoryName(Path.GetFullPath(mapPath))!;

var map = JsonSerializer.Deserialize<PackageMap>(
	File.ReadAllText(mapPath),
	new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip })
	?? throw new InvalidOperationException($"Could not read the package map at '{mapPath}'.");

var validator = new DependencyValidator(map);
var findings = new List<Finding>();

// locked package id -> version -> projects that resolved it
var lockObservations = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);

Console.WriteLine($"CodeBrix.Platform package dependency gate — {map.Packages.Count} package(s) mapped.");

foreach (var package in map.Packages)
{
	if (map.ExcludedPackages.Contains(package.PackageId, StringComparer.OrdinalIgnoreCase))
	{
		Console.WriteLine($"  SKIP  {package.PackageId} (excluded by the package map)");

		continue;
	}

	var referenced = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	var requiredFromProjectReferences = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

	foreach (var project in package.Projects)
	{
		var projectPath = Path.Combine(repoRoot, project);

		if (!File.Exists(projectPath))
		{
			Console.WriteLine($"  ERROR {package.PackageId}: mapped project '{project}' does not exist.");

			findings.Add(new Finding(
				FindingSeverity.Error,
				package.PackageId,
				"(project map)",
				$"Mapped project '{project}' does not exist. The package map is out of date."));

			continue;
		}

		foreach (var (id, version) in ProjectEvaluator.GetPackageReferences(projectPath, targetFramework))
		{
			referenced[id] = version;

			if (map.LockedPackages.Contains(id, StringComparer.OrdinalIgnoreCase))
			{
				if (!lockObservations.TryGetValue(id, out var byVersion))
				{
					byVersion = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
					lockObservations[id] = byVersion;
				}

				if (!byVersion.TryGetValue(version, out var projects))
				{
					projects = new List<string>();
					byVersion[version] = projects;
				}

				projects.Add(Path.GetFileName(project));
			}
		}

		foreach (var referencedProject in ProjectEvaluator.GetFlowingProjectReferences(projectPath, targetFramework))
		{
			var relative = Path
				.GetRelativePath(repoRoot, referencedProject)
				.Replace('\\', '/');

			if (!map.ProjectPackages.TryGetValue(relative, out var owningPackageId))
			{
				findings.Add(new Finding(
					FindingSeverity.Warning,
					package.PackageId,
					relative,
					$"'{project}' has a ProjectReference to '{relative}', which is not listed in "
					+ "projectPackages. If that project ships in a package, add it there so this gate can "
					+ "see the dependency; the map is otherwise silently incomplete."));

				continue;
			}

			// A reference to a project folded into THIS same package is not a dependency.
			if (!string.Equals(owningPackageId, package.PackageId, StringComparison.OrdinalIgnoreCase))
			{
				requiredFromProjectReferences.Add(owningPackageId);
			}
		}
	}

	if (emitPropertiesDir is not null)
	{
		NuspecPropertyWriter.Write(emitPropertiesDir, package, referenced, map.LockedPackages);
	}

	Dictionary<string, string> declared;

	if (packageDir is not null)
	{
		// Case-insensitive on purpose: packing happens on Windows and macOS as well as Linux, and
		// the produced file name casing must never decide whether the gate runs.
		var nupkg = Directory
			.EnumerateFiles(
				packageDir,
				$"{package.PackageId}.*.nupkg",
				new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })
			// "<id>.<version>.nupkg" only — never a longer package id that merely starts the same.
			.Where(f => char.IsDigit(Path.GetFileName(f)[(package.PackageId.Length + 1)]))
			.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
			.LastOrDefault();

		if (nupkg is null)
		{
			Console.WriteLine($"  ERROR {package.PackageId}: no .nupkg found in '{packageDir}'.");

			findings.Add(new Finding(
				FindingSeverity.Error,
				package.PackageId,
				"(package)",
				$"No produced .nupkg found in '{packageDir}'."));

			continue;
		}

		declared = NuspecReader.ReadFromPackage(nupkg);
	}
	else
	{
		declared = NuspecReader.ReadFromNuspec(Path.Combine(nuspecDir, package.Nuspec));
	}

	var packageFindings = validator
		.Validate(package, declared, referenced, requiredFromProjectReferences)
		.Concat(validator.ValidateProjectReferenceDependencies(package, declared, requiredFromProjectReferences))
		.ToList();

	findings.AddRange(packageFindings);

	Console.WriteLine(packageFindings.Count == 0
		? $"  OK    {package.PackageId}"
		: $"  FAIL  {package.PackageId} ({packageFindings.Count} finding(s))");
}

findings.AddRange(validator.ValidateLock(lockObservations));

if (map.LockDeclaration is { } lockDeclaration && lockDeclaration.Properties.Count > 0)
{
	var lockFile = Path.Combine(nuspecDir, lockDeclaration.File);

	if (!File.Exists(lockFile))
	{
		findings.Add(new Finding(
			FindingSeverity.Error,
			"(consumer lock)",
			lockDeclaration.File,
			$"The lock declaration file '{lockDeclaration.File}' does not exist."));
	}
	else
	{
		var properties = MsBuildPropertyReader.ReadProperties(lockFile);
		var declaredLockVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var (packageId, propertyName) in lockDeclaration.Properties)
		{
			if (properties.TryGetValue(propertyName, out var value))
			{
				declaredLockVersions[packageId] = value;
			}
			else
			{
				findings.Add(new Finding(
					FindingSeverity.Error,
					"(consumer lock)",
					packageId,
					$"'{lockDeclaration.File}' does not define the required-version property "
					+ $"'{propertyName}', so consuming applications have no lock for '{packageId}'."));
			}
		}

		findings.AddRange(validator.ValidateLockDeclaration(declaredLockVersions, lockObservations));
	}
}

Console.WriteLine();

foreach (var lockedId in map.LockedPackages)
{
	if (lockObservations.TryGetValue(lockedId, out var versions) && versions.Count == 1)
	{
		Console.WriteLine($"  LOCK  {lockedId} = {versions.Keys.First()}");
	}
}

var errors = findings.Count(f => f.Severity == FindingSeverity.Error);
var warnings = findings.Count(f => f.Severity == FindingSeverity.Warning);

if (findings.Count > 0)
{
	Console.WriteLine();

	foreach (var finding in findings.OrderByDescending(f => f.Severity == FindingSeverity.Error))
	{
		var label = finding.Severity == FindingSeverity.Error ? "error" : "warning";

		Console.WriteLine($"{label} : [{finding.PackageId}] {finding.Message}");
	}
}

Console.WriteLine();
Console.WriteLine($"Package dependency gate: {errors} error(s), {warnings} warning(s).");

return errors > 0 && !noFail ? 1 : 0;
