using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Contracts;
using CodeBrix.Platform.Foundation.Extensibility;
using SkiaSharp;

namespace CodeBrix.Platform.UI.PlotterView.Contracts;

/// <summary>
/// Resolves a platform contract of this assembly from the <see cref="ApiExtensibility"/> registry.
/// </summary>
/// <remarks>
/// Callers resolve a service contract once and keep it in a static field, and create a per-element hook once per
/// element and keep it in a field; neither is called per operation.
/// <para>
/// An application names the add-in's chart control (PlotterControl), which live in this (Core) assembly, so nothing else
/// makes the runtime load the platform assembly that implements the contracts. When a contract is not registered yet,
/// the platform assemblies of this library are therefore loaded by name and their module initializers (each one's
/// platform bootstrap) are run: <c>CodeBrix.Platform.UI.PlotterView</c> on CodeBrix.Platform, and, by the naming
/// rule that also reserves their InternalsVisibleTo grants (decision P4), <c>CodeBrix.Android.UI.PlotterView</c> and
/// <c>CodeBrix.Mobile.UI.PlotterView</c>. A name that is not present in the application is skipped.
/// </para>
/// <para>
/// The chart engine's one foreign contract, the platform's font source
/// (<c>CodeBrix.Platform.Foundation.Contracts.IFontSourcePlatform&lt;SKTypeface&gt;</c>, WPE1 C7), is registered by the
/// platform's framework or text assembly instead; <see cref="ResolveFontSource"/> runs those bootstraps
/// (<see cref="FontSourceAssemblyNames"/>) when it is not registered yet.
/// </para>
/// </remarks>
internal static class PlatformContract
{
	/// <summary>The assemblies that implement this library's platform contracts, one per platform.</summary>
	internal static readonly string[] PlatformAssemblyNames =
	{
		"CodeBrix.Platform.UI.PlotterView",
		"CodeBrix.Android.UI.PlotterView",
		"CodeBrix.Mobile.UI.PlotterView",
	};

	/// <summary>
	/// The assemblies that register the platform's font source: the ones TextLayout.Core's contract loads
	/// (<c>CodeBrix.Platform.UI</c>, <c>CodeBrix.Android.UI</c>, <c>CodeBrix.Android.UI.TextLayout</c>,
	/// <c>CodeBrix.Mobile.UI.TextLayout</c>), then this library's own platform assemblies.
	/// </summary>
	internal static readonly string[] FontSourceAssemblyNames =
	{
		"CodeBrix.Platform.UI",
		"CodeBrix.Android.UI",
		"CodeBrix.Android.UI.TextLayout",
		"CodeBrix.Mobile.UI.TextLayout",
		"CodeBrix.Android.UI.PlotterView",
		"CodeBrix.Mobile.UI.PlotterView",
	};

	/// <summary>
	/// Returns the platform's font source (the chart's typefaces, WPE1 C7), running the bootstraps of
	/// <see cref="FontSourceAssemblyNames"/> first when none is registered yet.
	/// </summary>
	/// <returns>The registered font source.</returns>
	/// <exception cref="InvalidOperationException">No font source is registered, and none of those assemblies
	/// registered one when loaded.</exception>
	internal static IFontSourcePlatform<SKTypeface> ResolveFontSource()
	{
		if (ApiExtensibility.CreateInstance<IFontSourcePlatform<SKTypeface>>(typeof(IFontSourcePlatform<SKTypeface>), out var source))
		{
			return source;
		}

		RunPlatformBootstraps(FontSourceAssemblyNames);

		if (ApiExtensibility.CreateInstance<IFontSourcePlatform<SKTypeface>>(typeof(IFontSourcePlatform<SKTypeface>), out source))
		{
			return source;
		}

		throw new InvalidOperationException(
			$"The platform contract {typeof(IFontSourcePlatform<SKTypeface>).FullName} is not registered. "
			+ "The platform bootstrap must run before it is used.");
	}

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

		RunPlatformBootstraps(PlatformAssemblyNames);

		if (ApiExtensibility.CreateInstance<TContract>(owner, out implementation))
		{
			return implementation;
		}

		throw new InvalidOperationException(
			$"The platform contract {typeof(TContract).FullName} is not registered. "
			+ "The platform bootstrap must run before it is used.");
	}

	private static void RunPlatformBootstraps(string[] assemblyNames)
	{
		foreach (var name in assemblyNames)
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
