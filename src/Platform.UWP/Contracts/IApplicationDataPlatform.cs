namespace CodeBrix.Platform.Contracts;

/// <summary>
/// Where the application data store lives on this platform: the folders behind
/// <see cref="Windows.Storage.ApplicationData"/> (LocalFolder, RoamingFolder, LocalCacheFolder, TemporaryFolder)
/// and the folder that holds the LocalSettings and RoamingSettings files.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Every method returns the full path of a folder that exists when the method returns (the implementation
/// creates it). <see cref="Windows.Storage.ApplicationData"/> asks for each folder lazily, the first time the
/// corresponding property is read.
/// </para>
/// <para>
/// Resolved once by <c>ApplicationData</c> (lazily, on first use) through
/// <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/>. The Skia implementation is
/// <c>CodeBrix.Platform.Skia.ApplicationDataSkiaPlatform</c>, registered by the assembly's
/// <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IApplicationDataPlatform
{
	/// <summary>
	/// Returns the folder behind <see cref="Windows.Storage.ApplicationData.LocalFolder"/>.
	/// </summary>
	/// <returns>The full path of an existing folder.</returns>
	string GetLocalFolderPath();

	/// <summary>
	/// Returns the folder behind <see cref="Windows.Storage.ApplicationData.RoamingFolder"/>.
	/// </summary>
	/// <returns>The full path of an existing folder.</returns>
	string GetRoamingFolderPath();

	/// <summary>
	/// Returns the folder behind <see cref="Windows.Storage.ApplicationData.LocalCacheFolder"/>.
	/// </summary>
	/// <returns>The full path of an existing folder.</returns>
	string GetLocalCacheFolderPath();

	/// <summary>
	/// Returns the folder behind <see cref="Windows.Storage.ApplicationData.TemporaryFolder"/>.
	/// </summary>
	/// <returns>The full path of an existing folder.</returns>
	string GetTemporaryFolderPath();

	/// <summary>
	/// Returns the folder that holds the files behind <see cref="Windows.Storage.ApplicationData.LocalSettings"/>
	/// and <see cref="Windows.Storage.ApplicationData.RoamingSettings"/>.
	/// </summary>
	/// <returns>The full path of an existing folder.</returns>
	string GetSettingsFolderPath();
}
