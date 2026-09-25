using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Toolkit.Contracts;

namespace CodeBrix.Platform.UI.Toolkit.Skia;

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

			var elevation = new ElevationSkiaPlatform();
			ApiExtensibility.Register(typeof(IElevationPlatform), _ => elevation);

			_registered = true;
		}
	}
}
