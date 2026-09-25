using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.VideoPlayer.Skia.Contracts;

namespace CodeBrix.Platform.UI.VideoPlayer.Skia;

/// <summary>
/// Registers this assembly's implementations of the VideoPlayer add-in's platform contracts with
/// <see cref="ApiExtensibility"/> (WPE1 C11: the asset location for the Core's source resolver). Runs as a module
/// initializer, so the registrations exist before any code of this assembly runs; the Core assembly's
/// <see cref="PlatformContract"/> runs it when it first needs a contract.
/// </summary>
internal static class SkiaPlatformBootstrap
{
	private static readonly object _gate = new();
	private static bool _registered;

	//A module initializer is the one place that runs before the first use of any type in this assembly,
	//which is exactly when the contracts must be in place (CA2255 is aimed at application-level code).
#pragma warning disable CA2255
	[ModuleInitializer]
#pragma warning restore CA2255
	internal static void Initialize() => EnsureRegistered();

	/// <summary>
	/// Registers every Skia contract implementation of this assembly. Safe to call more than once.
	/// </summary>
	internal static void EnsureRegistered()
	{
		lock (_gate)
		{
			if (_registered)
			{
				return;
			}

			//Where the application's assets are, for the source resolver (one for the process).
			var assets = new AssetLocationSkiaPlatform();
			ApiExtensibility.Register(typeof(IAssetLocation), _ => assets);

			_registered = true;
		}
	}
}
