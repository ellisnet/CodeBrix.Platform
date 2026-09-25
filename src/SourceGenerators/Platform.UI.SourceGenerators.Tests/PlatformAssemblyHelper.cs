using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.Verifiers; //Was previously: Uno.UI.SourceGenerators.Tests.Verifiers

internal static partial class CodeBrixAssemblyHelper
{
	public static PortableExecutableReference[] LoadAssemblies() =>
		[
			..LoadAssemblies(GetBinDirectory(
				"Platform.UI",
				"CodeBrix.Platform.UI.dll",
				[
					// The unit-test flavor: one assembly per library (the Skia/Reference flavors no longer hold the whole API since the Core/Skia split)
					"Platform.UI.Tests",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Platform.UWP",
				"CodeBrix.Platform.dll",
				[
					// The unit-test flavor: one assembly per library (the Skia/Reference flavors no longer hold the whole API since the Core/Skia split)
					"Platform.Tests",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Platform.Foundation",
				"CodeBrix.Platform.Foundation.dll",
				[
					// The unit-test flavor: one assembly per library (the Skia/Reference flavors no longer hold the whole API since the Core/Skia split)
					"Platform.Foundation.Tests",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Platform.UI.Composition",
				"CodeBrix.Platform.UI.Composition.dll",
				[
					// The unit-test flavor: one assembly per library (the Skia/Reference flavors no longer hold the whole API since the Core/Skia split)
					"Platform.UI.Composition.Tests",
				],
				[TFMPrevious, TFMCurrent]
			)),
			.. LoadAssemblies(GetBinDirectory(
				"Platform.UI.Toolkit",
				"CodeBrix.Platform.UI.Toolkit.dll",
				[
					// The unit-test flavor: one assembly per library (the Skia/Reference flavors no longer hold the whole API since the Core/Skia split)
					"Platform.UI.Toolkit.Tests",
				],
				[TFMPrevious, TFMCurrent]
			)),
		];

	public static PortableExecutableReference[] LoadAndroidAssemblies() =>
		LoadAssemblies(GetBinDirectory(
			"Platform.UI",
			"CodeBrix.Platform.UI.dll",
			["Platform.UI.netcoremobile"],
			[$"{TFMPrevious}-android", $"{TFMCurrent}-android"]
		));

	private static string GetBinDirectory(string baseName, string assemblyName, string[] targets, string[] tfms)
	{
		var tfmSubPaths =
		(
			from tfm in tfms
			from target in targets
			select Path.Combine(target, CurrentConfiguration, tfm)
		).ToArray();

		var codebrixBasePath = Path.Combine(
			Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
			"..",
			"..",
			"..",
			"..",
			"..",
			baseName,
			"bin"
		);

		var directory = tfmSubPaths
			.Select(x => Path.Combine(codebrixBasePath, x))
			.FirstOrDefault(x => File.Exists(Path.Combine(x, assemblyName)));
		if (directory is null)
		{
			throw new InvalidOperationException(string.Join("\n", (string[])[
				$"Unable to find {assemblyName} in the expected locations.",
#if DEBUG
				// on ci, they are ensured by the ci script
				"note: If you are getting this error locally, make sure to build the CodeBrix.Platform.UI project once for any of the target listed below",
#endif
				$"codebrixBasePath: {new Uri(codebrixBasePath).LocalPath}",
				$"tfmSubPaths:",
				..tfmSubPaths.Select(x => $"  - {x}"),
			]));
		}

		return directory;
	}

	private static PortableExecutableReference[] LoadAssemblies(string binDirectory) =>
		Directory.GetFiles(binDirectory, "*.dll")
			.Select(x => MetadataReference.CreateFromFile(x))
			.ToArray();
}

partial class CodeBrixAssemblyHelper
{
	private const string CurrentConfiguration =
#if DEBUG
		"Debug";
#else
		"Release";
#endif
	private const string TFMPrevious = "net9.0";
	private const string TFMCurrent = "net10.0";
}
