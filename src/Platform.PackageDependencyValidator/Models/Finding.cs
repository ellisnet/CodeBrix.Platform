namespace CodeBrix.Platform.PackageDependencyValidator.Models;

/// <summary>How badly a <see cref="Finding"/> should be treated by the pack driver.</summary>
public enum FindingSeverity
{
	/// <summary>Reported, does not fail the build.</summary>
	Warning,

	/// <summary>Fails the build.</summary>
	Error,
}

/// <summary>A single disagreement between a .nuspec and the project(s) it packs.</summary>
/// <param name="Severity">Whether this fails the build.</param>
/// <param name="PackageId">The package being validated.</param>
/// <param name="DependencyId">The dependency the finding concerns.</param>
/// <param name="Message">Human-readable explanation, already fully formed.</param>
public sealed record Finding(
	FindingSeverity Severity,
	string PackageId,
	string DependencyId,
	string Message);
