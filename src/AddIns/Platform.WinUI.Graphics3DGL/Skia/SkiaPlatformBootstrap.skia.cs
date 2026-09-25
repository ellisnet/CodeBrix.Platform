using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.WinUI.Graphics3DGL.Contracts;

namespace CodeBrix.Platform.WinUI.Graphics3DGL.Skia;

/// <summary>
/// Registers this assembly's implementations of the Graphics3DGL add-in's platform contracts with
/// <see cref="ApiExtensibility"/>. Runs as a module initializer, so the registrations exist before any code of this
/// assembly runs; the Core assembly's <see cref="PlatformContract"/> runs it when it first needs a contract.
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

			//One visual factory for the process; it creates one visual per GLCanvasElement.
			var canvas = new GLCanvasSkiaPlatform();
			ApiExtensibility.Register(typeof(IGLCanvasPlatform), _ => canvas);

			_registered = true;
		}
	}
}
