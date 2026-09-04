using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeBrix.Platform.Foundation.Extensibility;
using Windows.Graphics.Display;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// Puts a display scale under the suite's control, so a host-free test can measure the add-in at
/// something other than 100%.
/// </summary>
/// <remarks>
/// <para>
/// With no application head the Skia <c>DisplayInformation</c> finds no display-information
/// extension through the platform's extensibility registry, logs one warning and falls back to 96
/// logical dpi, so everything reports a scale of 1.0 and the scale-aware half of the add-in - an
/// icon rasterised at IconSize times the rasterization scale - is never exercised. This registers a
/// MUTABLE fake extension instead, from a module initializer, which runs before any test and
/// therefore before the first <c>DisplayInformation</c> is created and caches the instance the
/// registry hands it. Because the fake is a single mutable object, changing
/// <see cref="LogicalDpi"/> afterwards is seen live by that cached instance.
/// </para>
/// <para>
/// The extension interface is INTERNAL to the framework, and this suite deliberately does not ask
/// the framework to widen it with another InternalsVisibleTo grant for a test-only concern - the
/// same decision DispatcherInitializer records. The fake is therefore a
/// <see cref="DispatchProxy"/> over the interface type looked up by name, which implements an
/// internal interface from another assembly without any grant at all.
/// </para>
/// <para>
/// What is not necessarily live is a control that read the scale once when it was created, so a
/// test that wants a different scale should set it and then construct a NEW control. Use
/// <see cref="At"/>, which does that and restores the default in a finally.
/// </para>
/// </remarks>
internal static class DisplayScale
{
	/// <summary>The dpi a display at 100% reports; the framework's own base.</summary>
	internal const float BaseDpi = 96f;

	[ModuleInitializer]
	internal static void Initialize()
	{
		var extensionType = Type.GetType(
			"Windows.Graphics.Display.IDisplayInformationExtension, CodeBrix.Platform");
		if (extensionType is null)
		{
			throw new InvalidOperationException(
				"Could not find IDisplayInformationExtension in CodeBrix.Platform. The test "
				+ "project's display bootstrap needs updating to match the framework.");
		}

		var extension = DispatchProxy.Create(extensionType, typeof(DisplayInformationProxy));

		//Register once, before anything creates a DisplayInformation. ApiExtensibility.Register
		//throws on a duplicate key, which is the behaviour wanted here: if a head ever registered
		//one first, the suite must fail loudly rather than measure the wrong display.
		ApiExtensibility.Register(extensionType, _ => extension);
	}

	/// <summary>The logical dpi every <c>DisplayInformation</c> in this process now reports.</summary>
	internal static float LogicalDpi
	{
		get => DisplayInformationProxy.LogicalDpi;
		set => DisplayInformationProxy.LogicalDpi = value;
	}

	/// <summary>
	/// Runs <paramref name="body"/> with the display reporting <paramref name="scale"/> times 96
	/// dpi, then restores 96 dpi whatever happens.
	/// </summary>
	/// <param name="scale">The display scale, e.g. 2.0 for a 200% display.</param>
	/// <param name="body">What to measure. Construct the control INSIDE it.</param>
	internal static void At(double scale, Action body)
	{
		LogicalDpi = (float)(BaseDpi * scale);
		try
		{
			body();
		}
		finally
		{
			LogicalDpi = BaseDpi;
		}
	}

	/// <summary>
	/// The asynchronous form of <see cref="At"/>, for work that has to be awaited inside the scope.
	/// </summary>
	/// <param name="scale">The display scale, e.g. 2.0 for a 200% display.</param>
	/// <param name="body">What to measure. Everything that READS the scale must happen inside the
	/// returned task, since the scale is restored as soon as it completes.</param>
	/// <returns>A task that completes when the scale has been restored.</returns>
	internal static async Task AtAsync(double scale, Func<Task> body)
	{
		LogicalDpi = (float)(BaseDpi * scale);
		try
		{
			await body();
		}
		finally
		{
			LogicalDpi = BaseDpi;
		}
	}
}

/// <summary>
/// The dispatch proxy that answers the framework's internal display-information extension.
/// </summary>
/// <remarks>
/// It has to be public and non-sealed with a parameterless constructor, because
/// <see cref="DispatchProxy"/> emits a subclass of it. The one mutable value lives in a STATIC
/// field: the display scale is process-wide, exactly one proxy is ever created, and a static keeps
/// <see cref="DisplayScale.LogicalDpi"/> a plain property rather than another reflection call.
/// </remarks>
public class DisplayInformationProxy : DispatchProxy
{
	internal static float LogicalDpi = DisplayScale.BaseDpi;

	/// <summary>Answers one property getter of the extension interface.</summary>
	/// <param name="targetMethod">The interface member being called.</param>
	/// <param name="args">Its arguments; always empty, since the interface is properties only.</param>
	/// <returns>The value for that member.</returns>
	protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
	{
		//Only the interface itself is internal; the types on it are public, so the values below
		//are written plainly. The interface is properties only, hence the get_ prefixes.
		switch (targetMethod?.Name)
		{
			case "get_CurrentOrientation":
				return DisplayOrientations.Landscape;
			case "get_ScreenHeightInRawPixels":
				return 1080u;
			case "get_ScreenWidthInRawPixels":
				return 1920u;
			case "get_LogicalDpi":
				return LogicalDpi;
			case "get_RawPixelsPerViewPixel":
				return (double)LogicalDpi / DisplayScale.BaseDpi;
			case "get_ResolutionScale":
				return ResolutionScale.Scale100Percent;
			case "get_DiagonalSizeInInches":
				return 15.0d;
			default:
				throw new NotSupportedException(
					$"The suite's display-information fake does not answer '{targetMethod?.Name}'. "
					+ "The framework's extension interface has grown a member; add it here.");
		}
	}
}

/// <summary>
/// The collection for tests that change the display scale. The scale is process-wide, and so is the
/// one <c>DisplayInformation</c> instance the framework caches per window, so these tests must not
/// run beside anything that constructs a control.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DisplayScaleCollection
{
	/// <summary>The collection's name.</summary>
	public const string Name = "Display scale";
}
