using System.Diagnostics;
using System.Text.Json;

namespace CodeBrix.Platform.PackageDependencyValidator.Validation;

/// <summary>
/// Reads the EFFECTIVE PackageReference set of a project by asking MSBuild to evaluate it.
/// </summary>
/// <remarks>
/// This is deliberately the same view Visual Studio's Solution NuGet package view manages, which
/// is what makes the .csproj authoritative: versionless references that get their version from the
/// <c>PackageReference Update</c> block in src/Directory.Build.targets come back fully resolved
/// (e.g. SkiaSharp -&gt; 4.150.1), so nothing has to be duplicated or guessed here.
///
/// Evaluation only — no restore, no build, no network.
/// </remarks>
public static class ProjectEvaluator
{
	/// <summary>
	/// Evaluates <paramref name="projectPath"/> and returns its package id -&gt; version map.
	/// References carrying <c>PrivateAssets=All</c> (analyzers, source-link, compiler toolsets)
	/// are excluded: they are build-time only and must never become package dependencies.
	/// </summary>
	public static Dictionary<string, string> GetPackageReferences(string projectPath, string targetFramework)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		using var document = JsonDocument.Parse(RunGetItem(projectPath, "PackageReference", targetFramework));

		if (!document.RootElement.TryGetProperty("Items", out var items)
			|| !items.TryGetProperty("PackageReference", out var references))
		{
			return result;
		}

		foreach (var reference in references.EnumerateArray())
		{
			var id = reference.TryGetProperty("Identity", out var identity) ? identity.GetString() : null;

			if (string.IsNullOrWhiteSpace(id))
			{
				continue;
			}

			if (reference.TryGetProperty("PrivateAssets", out var privateAssets)
				&& string.Equals(privateAssets.GetString(), "All", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var version = reference.TryGetProperty("Version", out var v) ? v.GetString() : null;

			if (!string.IsNullOrWhiteSpace(version))
			{
				result[id] = version;
			}
		}

		return result;
	}

	private static string RunGetItem(string projectPath, string itemName, string targetFramework)
	{
		var psi = new ProcessStartInfo("dotnet")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath)),
		};
		psi.ArgumentList.Add("msbuild");
		psi.ArgumentList.Add(Path.GetFullPath(projectPath));
		psi.ArgumentList.Add($"-getItem:{itemName}");
		psi.ArgumentList.Add($"-p:TargetFramework={targetFramework}");
		psi.ArgumentList.Add("-nologo");

		using var process = Process.Start(psi)
			?? throw new InvalidOperationException($"Could not start MSBuild for '{projectPath}'.");

		var stdout = process.StandardOutput.ReadToEnd();
		var stderr = process.StandardError.ReadToEnd();
		process.WaitForExit();

		var braceIndex = stdout.IndexOf('{');

		if (process.ExitCode != 0 || braceIndex < 0)
		{
			throw new InvalidOperationException(
				$"MSBuild evaluation failed for '{projectPath}'.{Environment.NewLine}{stdout}{stderr}");
		}

		return stdout[braceIndex..];
	}

	/// <summary>
	/// Returns the full paths of the ProjectReferences of <paramref name="projectPath"/> that FLOW
	/// as package dependencies.
	/// </summary>
	/// <remarks>
	/// A ProjectReference to a project that ships in a DIFFERENT package is a real package
	/// dependency, but a PackageReference-based check cannot see it — that is how Svg and Lottie
	/// came to bind Graphics2DSK without declaring it.
	///
	/// References carrying <c>PrivateAssets=all</c> or <c>ReferenceOutputAssembly=false</c> are
	/// excluded, which is ordinary NuGet semantics: the first says "deliberately does not flow",
	/// the second is analyzer / source-generator wiring rather than a real reference.
	/// </remarks>
	public static IReadOnlyList<string> GetFlowingProjectReferences(string projectPath, string targetFramework)
	{
		var json = RunGetItem(projectPath, "ProjectReference", targetFramework);
		var result = new List<string>();

		using var document = JsonDocument.Parse(json);

		if (!document.RootElement.TryGetProperty("Items", out var items)
			|| !items.TryGetProperty("ProjectReference", out var references))
		{
			return result;
		}

		foreach (var reference in references.EnumerateArray())
		{
			if (reference.TryGetProperty("PrivateAssets", out var privateAssets)
				&& string.Equals(privateAssets.GetString(), "all", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (reference.TryGetProperty("ReferenceOutputAssembly", out var outputAssembly)
				&& string.Equals(outputAssembly.GetString(), "false", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (reference.TryGetProperty("FullPath", out var fullPath)
				&& fullPath.GetString() is { Length: > 0 } path)
			{
				result.Add(path);
			}
		}

		return result;
	}
}
