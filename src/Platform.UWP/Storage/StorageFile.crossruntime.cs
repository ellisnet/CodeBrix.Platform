#if !__NETSTD_REFERENCE__
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Helpers;
using Windows.ApplicationModel;

namespace Windows.Storage
{
	partial class StorageFile
	{
		internal static string ResourcePathBase { get; set; } = Package.Current.InstalledPath;

		private static async Task<StorageFile> GetFileFromApplicationUri(CancellationToken ct, Uri uri)
		{
			if (uri.Scheme != "ms-appx")
			{
				// ms-appdata is handled by the caller.
				throw new InvalidOperationException("Uri is not using the ms-appx or ms-appdata scheme");
			}

			// WPE1-13: a platform whose package files are not files on disk (Android's APK assets) serves them as
			// streams; unregistered (the Skia heads), the file under ResourcePathBase is used, as before.
			if (ApplicationPackageFiles.Platform is { } packageFiles)
			{
				var relativePath = ApplicationPackageFiles.GetRelativePath(uri);
				if (!packageFiles.FileExists(relativePath))
				{
					throw new FileNotFoundException($"The file [{relativePath}] cannot be found in the application package.", relativePath);
				}

				return StorageFile.FromImplementation(new PackageFile(packageFiles, relativePath));
			}

			var path = Uri.UnescapeDataString(uri.PathAndQuery).TrimStart('/');

			// WPE1-14: the host (a library's folder) as written in the URI when that path exists, else lower-cased as before.
			var resourcePathname = global::System.IO.Path.Combine(ResourcePathBase, InstalledPackagePath.ResolveHost(uri, ResourcePathBase, path), path);

			if (resourcePathname != null)
			{
				return await StorageFile.GetFileFromPathAsync(resourcePathname);
			}
			else
			{
				throw new FileNotFoundException($"The file [{path}] cannot be found  in the package directory");
			}
		}
	}
}
#endif
