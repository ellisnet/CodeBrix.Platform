#if !__NETSTD_REFERENCE__
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Graphics.Display;

namespace CodeBrix.Platform.Helpers; //Was previously: Uno.Helpers

internal static partial class PlatformImageHelpers
{
	// TODO: Introduce LRU caching if needed
	private static readonly Dictionary<string, string> _scaledBitmapCache = new();

	internal static Task<string> GetScaledPath(Uri uri, ResolutionScale? scaleOverride)
	{
		var path = uri.PathAndQuery;
		if (uri.Host is { Length: > 0 })
		{
			// WPE1-14: the host as written when that path exists under the installed folder, else lower-cased as before
			// (with a registered package-files platform the path below is only the cache key).
			var host = ApplicationPackageFiles.Platform is null
				? InstalledPackagePath.ResolveHost(uri, Package.Current.InstalledPath, path)
				: uri.Host;
			path = host + "/" + path.TrimStart('/');
		}

		// Avoid querying filesystem if we already seen this file
		if (_scaledBitmapCache.TryGetValue(path, out var result))
		{
			return Task.FromResult(result);
		}

		// WPE1-13: with a registered package-files platform the scale variants are probed in the package and the
		// result is an ms-appx URI (opened through the same platform); unregistered, a file path, as before.
		if (ApplicationPackageFiles.Platform is { } packageFiles)
		{
#pragma warning disable RS0030 // Do not use banned APIs // same as the file-path branch below
			var scale = (int)(scaleOverride ?? DisplayInformation.GetForCurrentView().ResolutionScale);
#pragma warning restore RS0030
			result = ApplicationPackageFiles.ToUriString(ApplicationPackageFiles.FindScaledPath(packageFiles, ApplicationPackageFiles.GetRelativePath(uri), scale, KnownScales));
			_scaledBitmapCache[path] = result;
			return Task.FromResult(result);
		}

		var originalLocalPath =
			Path.Combine(Package.Current.InstalledPath,
				 path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
			);

#pragma warning disable RS0030 // Do not use banned APIs // TODO MZ: Avoid this by using XamlRoot
		var resolutionScale = (int)(scaleOverride ?? DisplayInformation.GetForCurrentView().ResolutionScale);
#pragma warning restore RS0030 // Do not use banned APIs
		var baseDirectory = Path.GetDirectoryName(originalLocalPath);
		var baseFileName = Path.GetFileNameWithoutExtension(originalLocalPath);
		var baseExtension = Path.GetExtension(originalLocalPath);
		var applicableScale = FindApplicableScale(true);
		if (applicableScale is null)
		{
			applicableScale = FindApplicableScale(false);
		}

		result = applicableScale ?? originalLocalPath;
		_scaledBitmapCache[path] = result;
		return Task.FromResult(result);

		string FindApplicableScale(bool onlyMatching)
		{
			for (var i = KnownScales.Length - 1; i >= 0; i--)
			{
				var probeScale = KnownScales[i];
				if ((onlyMatching && resolutionScale >= probeScale) ||
					(!onlyMatching && resolutionScale < probeScale))
				{
					var filePath = Path.Combine(baseDirectory, $"{baseFileName}.scale-{probeScale}{baseExtension}");
					if (File.Exists(filePath))
					{
						return filePath;
					}
				}
			}
			return null;
		}
	}
}
#endif
