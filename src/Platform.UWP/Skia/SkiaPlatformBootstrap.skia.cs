using System.Runtime.CompilerServices;
using CodeBrix.Platform.Contracts;
using CodeBrix.Platform.Foundation.Extensibility;

namespace CodeBrix.Platform.Skia;

/// <summary>
/// Registers this assembly's Skia implementations of its platform contracts with
/// <see cref="ApiExtensibility"/>. Runs as a module initializer, so the registrations exist before any code of
/// this assembly runs.
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

			var applicationData = new ApplicationDataSkiaPlatform();
			ApiExtensibility.Register(typeof(IApplicationDataPlatform), _ => applicationData);

			var globalizationPreferences = new GlobalizationPreferencesSkiaPlatform();
			ApiExtensibility.Register(typeof(IGlobalizationPreferencesPlatform), _ => globalizationPreferences);

			var graphicsImaging = new GraphicsImagingSkiaPlatform();
			ApiExtensibility.Register(typeof(IGraphicsImagingPlatform), _ => graphicsImaging);

			_registered = true;
		}
	}
}
