#nullable enable

using System;
using System.IO;

namespace CodeBrix.Platform.UI.Tasks.LinkerHintsGenerator //Was previously: Uno.UI.Tasks.LinkerHintsGenerator
{
	/// <summary>
	/// Maps a reference path that points into the framework package's <c>lib/&lt;tfm&gt;</c> folder to the runtime copy of
	/// the same assembly under <c>codebrix-platform-runtime/&lt;tfm&gt;/&lt;runtime&gt;</c>, when that copy exists.
	/// </summary>
	/// <remarks>
	/// Only the Skia assemblies have a runtime copy. The platform-neutral <c>*.Core.dll</c> assemblies ship in
	/// <c>lib/</c> only, so their path is always returned unchanged.
	/// </remarks>
	internal static class RuntimeReferencePathRewriter
	{
		/// <summary>
		/// Returns the runtime-folder path of <paramref name="referencePath"/> when it has one, else the path itself.
		/// </summary>
		/// <param name="referencePath">The resolved reference path.</param>
		/// <param name="packageBasePath">The folder of the framework package in the package cache.</param>
		/// <param name="runtimeIdentifier">The CodeBrix runtime identifier of the build (for example <c>Skia</c>).</param>
		/// <param name="targetFrameworkVersion">The target framework version without its leading <c>v</c> (for example <c>10.0</c>).</param>
		/// <param name="fileExists">Checks whether a file exists (the file system, or a test double).</param>
		/// <returns>The path to use.</returns>
		internal static string Rewrite(string referencePath, string packageBasePath, string runtimeIdentifier, string targetFrameworkVersion, Func<string, bool> fileExists)
		{
			var separator = Path.DirectorySeparatorChar;
			runtimeIdentifier = runtimeIdentifier.ToLowerInvariant();

			// The package folders are named after the target framework the assemblies were built for (lib/net10.0 and
			// codebrix-platform-runtime/net10.0/skia); this used to be a fixed "net9.0", which matched no folder once
			// the framework moved to net10.0.
			var version = new Version(targetFrameworkVersion);
			var runtimeTargetFramework = version >= new Version("9.0")
				? $"net{version.Major}.{version.Minor}"
				: "netstandard2.0";

			var isCodeBrixRuntimeEnabled = (runtimeIdentifier == "skia" || runtimeIdentifier == "webassembly")
				&& referencePath.StartsWith(packageBasePath, StringComparison.Ordinal)
				// The Core assemblies have no runtime copy: they are the same for every runtime.
				&& !Path.GetFileNameWithoutExtension(referencePath).EndsWith(".Core", StringComparison.Ordinal);

			if (isCodeBrixRuntimeEnabled)
			{
				var originalFolderPath = $"lib{separator}{runtimeTargetFramework}";
				var preCodeBrix46FolderPart = $"codebrix-platform-runtime{separator}{runtimeIdentifier}";
				var postCodeBrix46FolderPathPart = $"codebrix-platform-runtime{separator}{runtimeTargetFramework}{separator}{runtimeIdentifier}";

				var post46Path = referencePath.Replace(originalFolderPath, postCodeBrix46FolderPathPart);
				var pre46Path = referencePath.Replace(originalFolderPath, preCodeBrix46FolderPart);

				if (fileExists(post46Path))
				{
					return post46Path;
				}
				else if (fileExists(pre46Path))
				{
					return pre46Path;
				}
			}

			return referencePath;
		}
	}
}
