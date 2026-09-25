using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeBrix.Platform.UWPSyncGenerator
{
	/// <summary>
	/// Builds the reference list of the WinRT/WinAppSDK API surface from the NuGet packages cache, as a
	/// platform-neutral replacement for the references.txt file exported by the Windows-only
	/// Platform.UWPSyncGenerator.Reference project.
	/// </summary>
	/// <remarks>
	/// The default set ("1.8": Windows SDK 10.0.22000 contracts, Windows App SDK 1.8) reproduces the checked-in
	/// Generated folders. Set the CODEBRIX_SYNCGEN_WINAPPSDK environment variable to "2.0.1" to generate against
	/// Windows App SDK 2.0.1 (the version pinned by the Reference project), which adds the 2.0 API surface.
	/// </remarks>
	internal static class WinRTReferenceList
	{
		// (package id, version, folder inside the package, file pattern)
		private static readonly (string Id, string Version, string Folder, string Pattern)[] _common =
		[
			// The .NET projections of the WinRT types (System.Runtime.WindowsRuntime, System.Numerics.Vectors, ...).
			("Microsoft.NETCore.UniversalWindowsPlatform", "6.2.14", "ref/uap10.0.15138", "*.dll"),

			// The Windows SDK API contracts (Windows.Foundation.UniversalApiContract, Windows.Phone.PhoneContract, ...).
			("Microsoft.Windows.SDK.Contracts", "10.0.22000.196", "ref/netstandard2.0", "*.winmd"),
		];

		private static readonly (string Id, string Version, string Folder, string Pattern)[] _winAppSdk18 =
		[
			("Microsoft.WindowsAppSDK.Foundation", "1.8.260203002", "metadata", "*.winmd"),
			("Microsoft.WindowsAppSDK.InteractiveExperiences", "1.8.260125001", "metadata/10.0.18362.0", "*.winmd"),
			("Microsoft.WindowsAppSDK.WinUI", "1.8.260204000", "metadata", "*.winmd"),
			("Microsoft.WindowsAppSDK.Widgets", "1.8.251231004", "metadata", "*.winmd"),
			("Microsoft.WindowsAppSDK.AI", "1.8.47", "metadata", "*.winmd"),
			("Microsoft.Web.WebView2", "1.0.3179.45", "lib", "Microsoft.Web.WebView2.Core.winmd"),
		];

		private static readonly (string Id, string Version, string Folder, string Pattern)[] _winAppSdk201 =
		[
			("Microsoft.WindowsAppSDK.Foundation", "2.0.20", "metadata", "*.winmd"),
			("Microsoft.WindowsAppSDK.InteractiveExperiences", "2.0.12", "metadata/10.0.18362.0", "*.winmd"),
			("Microsoft.WindowsAppSDK.WinUI", "2.0.12", "metadata", "*.winmd"),
			("Microsoft.WindowsAppSDK.Widgets", "2.0.4", "metadata", "*.winmd"),
			("Microsoft.WindowsAppSDK.AI", "2.0.185", "metadata", "*.winmd"),
			("Microsoft.Web.WebView2", "1.0.3719.77", "lib", "Microsoft.Web.WebView2.Core.winmd"),
		];

		public static string[] Build()
		{
			var packagesRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

			if (string.IsNullOrEmpty(packagesRoot))
			{
				packagesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
			}

			var winAppSdk = Environment.GetEnvironmentVariable("CODEBRIX_SYNCGEN_WINAPPSDK") switch
			{
				null or "" or "1.8" => _winAppSdk18,
				"2.0.1" => _winAppSdk201,
				var other => throw new InvalidOperationException($"Unsupported CODEBRIX_SYNCGEN_WINAPPSDK value '{other}' (expected 1.8 or 2.0.1)."),
			};

			var options = new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive };
			var references = new List<string>();

			foreach (var (id, version, folder, pattern) in _common.Concat(winAppSdk))
			{
				var path = Path.Combine(packagesRoot, id.ToLowerInvariant(), version, folder);

				if (!Directory.Exists(path))
				{
					throw new InvalidOperationException(
						$"The package {id} {version} was not found in the NuGet packages cache ({path}). " +
						$"Download it first, e.g. with <PackageDownload Include=\"{id}\" Version=\"[{version}]\" /> in a scratch project.");
				}

				references.AddRange(Directory.EnumerateFiles(path, pattern, options).OrderBy(f => f, StringComparer.Ordinal));
			}

			return references.ToArray();
		}
	}
}
