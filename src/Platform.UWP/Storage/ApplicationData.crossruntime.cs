#if !__NETSTD_REFERENCE__
#nullable enable

using System.Threading.Tasks;
using CodeBrix.Platform;
using CodeBrix.Platform.Contracts;

namespace Windows.Storage;

partial class ApplicationData
{
	private static IApplicationDataPlatform? _applicationDataPlatform;

	/// <summary>
	/// Gets the platform that decides where the application data store lives, resolved once on first use.
	/// </summary>
	private static IApplicationDataPlatform ApplicationDataPlatform =>
		_applicationDataPlatform ??= PlatformContract.Resolve<IApplicationDataPlatform>();

	internal Task EnablePersistenceAsync() => Task.CompletedTask;

	partial void InitializePartial() => WinRTFeatureConfiguration.ApplicationData.IsApplicationDataInitialized = true;

	private static string GetLocalCacheFolder() => ApplicationDataPlatform.GetLocalCacheFolderPath();

	private static string GetTemporaryFolder() => ApplicationDataPlatform.GetTemporaryFolderPath();

	private static string GetLocalFolder() => ApplicationDataPlatform.GetLocalFolderPath();

	private static string GetRoamingFolder() => ApplicationDataPlatform.GetRoamingFolderPath();

	internal string GetSettingsFolderPath() => ApplicationDataPlatform.GetSettingsFolderPath();
}
#endif
