using System;
using System.Reflection;
using CodeBrix.Platform.UI.Hosting;
using SkiaSharp.Views.Windows;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// Turns the opt-in direct present path on for the length of one test, and off again afterwards.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DirectSkiaCanvasMode"/> is a ONE-WAY latch by design: an application calls
/// <c>UseDirectSkiaCanvasMode()</c> once at host build and then runs in that mode for its whole
/// lifetime, so the shipped type has an internal <c>Enable()</c> and no way back. That is correct
/// for an application and impossible for a test suite, which has to measure both paths in one
/// process - so the private latch field is set and reset here by reflection, the same test-only
/// discipline DispatcherInitializer uses to install the dispatcher overrides a head would install.
/// </para>
/// <para>
/// Nothing about the shipped latch changes: it is still one-way, its <c>Enable()</c> is still
/// internal, and <see cref="DirectSkiaCanvasModeTests"/> asserts both of those.
/// </para>
/// </remarks>
internal static class DirectSkiaCanvasModeSwitch
{
	private static readonly FieldInfo Latch =
		typeof(DirectSkiaCanvasMode).GetField("_isEnabled", BindingFlags.NonPublic | BindingFlags.Static)
		?? throw new InvalidOperationException(
			"DirectSkiaCanvasMode no longer has a private static _isEnabled latch. The test "
			+ "project's direct-mode switch needs updating to match the framework.");

	/// <summary>Runs <paramref name="body"/> with the direct present path on, then turns it off.</summary>
	/// <param name="body">What to measure.</param>
	internal static void Enabled(Action body)
	{
		var previous = DirectSkiaCanvasMode.IsEnabled;
		Latch.SetValue(null, true);
		try
		{
			if (!DirectSkiaCanvasMode.IsEnabled)
			{
				throw new InvalidOperationException(
					"Setting DirectSkiaCanvasMode's private latch did not turn the mode on. The "
					+ "test project's direct-mode switch needs updating to match the framework.");
			}

			body();
		}
		finally
		{
			Latch.SetValue(null, previous);
		}
	}

	/// <summary>The canvas's staging pixel array, which the direct path must never allocate.</summary>
	/// <param name="canvas">The canvas to look inside.</param>
	internal static byte[]? StagingPixels(this SKXamlCanvas canvas)
	{
		var field = typeof(SKXamlCanvas).GetField("pixels", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException(
				"SKXamlCanvas no longer has a private `pixels` staging array. The test project's "
				+ "direct-mode assertions need updating to match the add-in.");
		return (byte[]?)field.GetValue(canvas);
	}
}

/// <summary>
/// The collection for tests that turn the direct present path on. The latch is process-wide, so
/// these tests must not run beside anything else that paints.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DirectSkiaCanvasModeCollection
{
	/// <summary>The collection's name.</summary>
	public const string Name = "Direct Skia canvas mode";
}
