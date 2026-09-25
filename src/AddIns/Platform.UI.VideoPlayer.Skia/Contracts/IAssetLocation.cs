using System.Reflection;

namespace CodeBrix.Platform.UI.VideoPlayer.Skia.Contracts;

/// <summary>
/// Where an application's assets are (WPE1 C11): the folder an ms-appx:/// URI resolves under, and the assembly an
/// embedded://./... URI names. The source resolver (Internal/VideoSourceResolver) asks this instead of the WinRT Package
/// and the XAML Application, so it runs without the XAML object model. (The AudioPlayer add-in carries the same contract
/// for its own resolver: add-ins share no code.)
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once, lazily, by the resolver through <see cref="PlatformContract.Resolve{TContract}"/>. The Skia
/// implementation is <c>CodeBrix.Platform.UI.VideoPlayer.Skia.AssetLocationSkiaPlatform</c> (Package.Current.InstalledPath,
/// Application.Current's assembly - exactly what the resolver read before), registered by the Skia assembly's
/// <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IAssetLocation
{
	/// <summary>The application's installed (package) folder: the root of ms-appx:/// asset paths.</summary>
	string InstalledPath { get; }

	/// <summary>The application's own assembly: the one an embedded://./Resource.Name URI names.</summary>
	Assembly ApplicationAssembly { get; }
}
