using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Extensibility;

namespace CodeBrix.Platform.WinUI.Graphics3DGL.Contracts;

/// <summary>
/// Resolves a platform contract of this assembly from the <see cref="ApiExtensibility"/> registry.
/// </summary>
/// <remarks>
/// Callers resolve a service contract once and keep it in a static field, and create a per-element hook once per
/// element and keep it in a field; neither is called per operation.
/// <para>
/// An application names the add-in's GLCanvasElement, which lives in this (Core) assembly, so nothing else
/// makes the runtime load the platform assembly that implements the contracts. When a contract is not registered yet,
/// the platform assemblies of this library are therefore loaded by name and their module initializers (each one's
/// platform bootstrap) are run: <c>CodeBrix.Platform.WinUI.Graphics3DGL</c> on CodeBrix.Platform, and, by the naming
/// rule that also reserves their InternalsVisibleTo grants (decision P4), <c>CodeBrix.Android.UI.Graphics3DGL</c> and
/// <c>CodeBrix.Mobile.UI.Graphics3DGL</c>. A name that is not present in the application is skipped.
/// </para>
/// </remarks>
internal static class PlatformContract
{
	/// <summary>The assemblies that implement this library's platform contracts, one per platform.</summary>
	internal static readonly string[] PlatformAssemblyNames =
	{
		"CodeBrix.Platform.WinUI.Graphics3DGL",
		"CodeBrix.Android.UI.Graphics3DGL",
		"CodeBrix.Mobile.UI.Graphics3DGL",
	};

	/// <summary>
	/// Returns the registered implementation of the platform service contract <typeparamref name="TContract"/>.
	/// </summary>
	/// <typeparam name="TContract">The contract interface.</typeparam>
	/// <returns>The implementation registered by the platform bootstrap.</returns>
	/// <exception cref="InvalidOperationException">No implementation of <typeparamref name="TContract"/> is registered,
	/// and none of the platform assemblies registered one when loaded.</exception>
	internal static TContract Resolve<TContract>()
		where TContract : class
		=> Create<TContract>(typeof(TContract));

	/// <summary>
	/// Creates the platform's per-element hook <typeparamref name="TContract"/> for <paramref name="owner"/>.
	/// </summary>
	/// <typeparam name="TContract">The contract interface.</typeparam>
	/// <param name="owner">The element the hook belongs to.</param>
	/// <returns>A new hook created by the builder the platform bootstrap registered.</returns>
	/// <exception cref="InvalidOperationException">No implementation of <typeparamref name="TContract"/> is registered,
	/// and none of the platform assemblies registered one when loaded.</exception>
	internal static TContract Create<TContract>(object owner)
		where TContract : class
	{
		if (ApiExtensibility.CreateInstance<TContract>(owner, out var implementation))
		{
			return implementation;
		}

		RunPlatformBootstraps();

		if (ApiExtensibility.CreateInstance<TContract>(owner, out implementation))
		{
			return implementation;
		}

		throw new InvalidOperationException(
			$"The platform contract {typeof(TContract).FullName} is not registered. "
			+ "The platform bootstrap must run before it is used.");
	}

	private static void RunPlatformBootstraps()
	{
		foreach (var name in PlatformAssemblyNames)
		{
			Assembly assembly;
			try
			{
				assembly = Assembly.Load(new AssemblyName(name));
			}
			catch (FileNotFoundException)
			{
				continue;
			}

			//Loading an assembly does not run its module initializer; this does (once per module).
			RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
		}
	}
}
