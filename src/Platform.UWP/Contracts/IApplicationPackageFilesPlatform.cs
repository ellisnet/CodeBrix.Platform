#nullable enable

using System.IO;

namespace CodeBrix.Platform.Contracts;

/// <summary>
/// The application package's own files (the files an <c>ms-appx:///</c> URI names) as STREAMS, for a platform whose
/// package files are not files on disk - an Android APK's assets, for one.
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile. Platform (Skia): not registered - the package files are files under
/// <see cref="Windows.ApplicationModel.Package.InstalledPath"/>, and every Core caller keeps using those paths, as before.
/// <para>
/// OPTIONAL contract (WPE1-13), looked up through <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/>
/// by <see cref="CodeBrix.Platform.Helpers.ApplicationPackageFiles"/> each time a Core caller turns an ms-appx URI into
/// a file (opening a package file is IO; the lookup is not on a per-frame path). While it is registered:
/// StorageFile.GetFileFromApplicationUriAsync returns a read-only package file whose streams come from
/// <see cref="OpenRead"/> (its Path is the ms-appx URI, not a file path); RandomAccessStreamReference.CreateFromUri,
/// the image loaders (BitmapImage, LoadedImageSurface, including the .scale-NNN probe) and the font-manifest check
/// read through it too; so does every caller that goes through StorageFile (the Lottie add-in, FontFamilyHelper,
/// AppDataUriEvaluator.ToStream).
/// </para>
/// <para>
/// A <paramref name="relativePath"/> is the URI's host (when it has one) and path, percent-escapes decoded, joined
/// with '/', without a leading '/': <c>ms-appx:///Assets/pulse.json</c> is <c>Assets/pulse.json</c>,
/// <c>ms-appx://MyLibrary/Fonts/a.ttf</c> is <c>MyLibrary/Fonts/a.ttf</c> - the same relative path the desktop heads
/// combine with the installed folder, and the path CodeBrix.Android's build gives every packaged file in the APK's
/// assets. Implementations are called from any thread.
/// </para>
/// </remarks>
internal interface IApplicationPackageFilesPlatform
{
	/// <summary>
	/// Returns whether the application package holds a file at <paramref name="relativePath"/>.
	/// </summary>
	/// <param name="relativePath">The package-relative path ('/'-separated, no leading '/').</param>
	/// <returns><see langword="true"/> when the file exists.</returns>
	bool FileExists(string relativePath);

	/// <summary>
	/// Opens the package file at <paramref name="relativePath"/> for reading.
	/// </summary>
	/// <param name="relativePath">The package-relative path ('/'-separated, no leading '/').</param>
	/// <returns>A readable stream the caller disposes (it need not be seekable: Core copies a non-seekable stream into
	/// memory where it needs to seek), or <see langword="null"/> when there is no such file.</returns>
	Stream? OpenRead(string relativePath);
}
