#nullable enable

using System;
using System.Reflection;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Turns the direct GPU rendering latch on for the length of one test, and puts it back afterwards.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DirectGpuRenderingMode"/> is a ONE-WAY latch by design: an application calls
/// <c>UseDirectGpuRenderingMode()</c> once at host build and then runs in that mode for its whole lifetime,
/// so the shipped type has an internal <c>Enable()</c> and no way back. That is correct for an application
/// and impossible for a test suite, which has to see both states in one process - so the private latch
/// field is reset here by reflection, the same discipline the SkiaSharp views suite uses for
/// DirectSkiaCanvasMode.
/// </para>
/// <para>
/// Nothing about the shipped latch changes: it is still one-way, its <c>Enable()</c> is still internal, and
/// <see cref="DirectGpuRenderingModeTests"/> asserts both of those.
/// </para>
/// </remarks>
internal static class DirectGpuRenderingModeSwitch
{
	private static readonly FieldInfo Latch =
		typeof(DirectGpuRenderingMode).GetField("_isEnabled", BindingFlags.NonPublic | BindingFlags.Static)
		?? throw new InvalidOperationException(
			"DirectGpuRenderingMode no longer has a private static _isEnabled latch. The test "
			+ "project's direct-GPU switch needs updating to match the framework.");

	/// <summary>Runs <paramref name="body"/> with the latch on, then restores its previous state.</summary>
	/// <param name="body">What to check while the mode is on.</param>
	internal static void Enabled(Action body) => Restoring(() =>
	{
		Latch.SetValue(null, true);
		if (!DirectGpuRenderingMode.IsEnabled)
		{
			throw new InvalidOperationException(
				"Setting DirectGpuRenderingMode's private latch did not turn the mode on. The "
				+ "test project's direct-GPU switch needs updating to match the framework.");
		}

		body();
	});

	/// <summary>Runs <paramref name="body"/> (which may latch the mode on) and restores the latch afterwards.</summary>
	/// <param name="body">What to run.</param>
	internal static void Restoring(Action body)
	{
		var previous = DirectGpuRenderingMode.IsEnabled;
		try
		{
			body();
		}
		finally
		{
			Latch.SetValue(null, previous);
		}
	}
}
