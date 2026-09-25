using System.Reflection;
using CodeBrix.Platform.UI.VideoPlayer.Skia.Contracts;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace CodeBrix.Platform.UI.VideoPlayer.Skia;

/// <summary>
/// The CodeBrix.Platform implementation of <see cref="IAssetLocation"/> (WPE1 C11): the WinRT package folder and the
/// running XAML application's assembly, read on each request as the source resolver always read them.
/// </summary>
internal sealed class AssetLocationSkiaPlatform : IAssetLocation
{
	/// <inheritdoc/>
	public string InstalledPath => Package.Current.InstalledPath;

	/// <inheritdoc/>
	public Assembly ApplicationAssembly => Application.Current.GetType().Assembly;
}
