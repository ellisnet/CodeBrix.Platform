#nullable enable

using System;
using System.Linq;
using SilverAssertions;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The assertion every engine test ends with: no WinUI assembly is loaded in this process. This is the proof that a
/// CodeBrix.Mobile app, which calls only engine types, never loads the XAML object model even though the engine's
/// Core assembly references it.
/// </summary>
internal static class EngineIsolation
{
	/// <summary>The WinUI assemblies, by name, that an engine must never cause to load.</summary>
	internal static readonly string[] WinUIAssemblies =
	{
		"CodeBrix.Platform.UI.Core",
		"CodeBrix.Platform.UI.Composition.Core",
		"CodeBrix.Platform.Core",
		"CodeBrix.Platform.UI.Dispatching.Core",
		"CodeBrix.Platform.Xaml",
		"CodeBrix.Platform.UI.FluentTheme.Core",
		"CodeBrix.Platform.UI.FluentTheme.v1.Core",
		"CodeBrix.Platform.UI.FluentTheme.v2.Core",
	};

	/// <summary>
	/// The CodeBrix.Platform assemblies an engine may load: the registry and logging base, and the engine Cores
	/// themselves. Any other CodeBrix.Platform assembly (a Skia twin such as CodeBrix.Platform.UI, a head) fails.
	/// </summary>
	private static readonly string[] AllowedPlatformAssemblies =
	{
		"CodeBrix.Platform.Foundation.Core",
		"CodeBrix.Platform.Foundation.Logging.Core",
		"CodeBrix.Platform.Extensions.Logging.Core",
		"CodeBrix.Platform.Extensions",
		"CodeBrix.Platform.UI.Engine.Tests",
	};

	/// <summary>The CodeBrix.Platform assembly names loaded in this process right now.</summary>
	internal static string[] LoadedPlatformAssemblies()
		=> AppDomain.CurrentDomain.GetAssemblies()
			.Select(a => a.GetName().Name ?? string.Empty)
			.Where(n => n.StartsWith("CodeBrix.Platform", StringComparison.Ordinal))
			.OrderBy(n => n, StringComparer.Ordinal)
			.ToArray();

	/// <summary>
	/// Asserts that no WinUI assembly and no Skia twin is loaded; only the base assemblies and engine Cores are.
	/// </summary>
	/// <param name="engine">The engine the calling test drove (named in the failure message).</param>
	internal static void AssertNoWinUILoaded(string engine)
	{
		var loaded = LoadedPlatformAssemblies();
		var forbidden = FindForbidden(loaded);

		forbidden.Should().BeEmpty(
			$"driving the {engine} engine must not load the XAML object model or a Skia twin (loaded CodeBrix.Platform assemblies: {string.Join(", ", loaded)})");
	}

	/// <summary>
	/// Returns the names in <paramref name="loadedNames"/> an engine must not load: the WinUI assemblies, and every
	/// other CodeBrix.Platform assembly that is neither a base assembly nor an engine Core (a Skia twin, a head).
	/// </summary>
	/// <param name="loadedNames">Loaded assembly names.</param>
	/// <returns>The forbidden names, in the given order.</returns>
	internal static string[] FindForbidden(System.Collections.Generic.IEnumerable<string> loadedNames)
		=> loadedNames
			.Where(n => n.StartsWith("CodeBrix.Platform", StringComparison.Ordinal))
			.Where(n => WinUIAssemblies.Contains(n, StringComparer.Ordinal)
				|| (!AllowedPlatformAssemblies.Contains(n, StringComparer.Ordinal) && !IsEngineCore(n)))
			.ToArray();

	/// <summary>An add-in Core (the engines live there): a CodeBrix.Platform assembly named *.Core that is not a WinUI assembly.</summary>
	private static bool IsEngineCore(string name)
		=> name.EndsWith(".Core", StringComparison.Ordinal) && !WinUIAssemblies.Contains(name, StringComparer.Ordinal);
}
