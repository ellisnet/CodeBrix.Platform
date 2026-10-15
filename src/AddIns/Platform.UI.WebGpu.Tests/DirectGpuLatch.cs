using System;
using System.Reflection;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// Sets the application-wide direct GPU rendering latch for the length of one test and puts it back afterwards.
/// </summary>
/// <remarks>
/// <see cref="DirectGpuRenderingMode"/> is a ONE-WAY latch by design (an application turns it on once, at host build),
/// which a test suite that must see both states in one process cannot use as shipped - so the private latch field is
/// set by reflection, the discipline Platform.UI.Core.Tests' DirectGpuRenderingModeSwitch uses. The suite runs one
/// test at a time (AssemblyInfo.cs), so no other test sees the latch flip.
/// </remarks>
internal sealed class DirectGpuLatch : IDisposable
{
	private static readonly FieldInfo Latch =
		typeof(DirectGpuRenderingMode).GetField("_isEnabled", BindingFlags.NonPublic | BindingFlags.Static)
		?? throw new InvalidOperationException(
			"DirectGpuRenderingMode no longer has a private static _isEnabled latch; the suite's latch switch needs updating.");

	private readonly bool _previous;

	private DirectGpuLatch(bool value)
	{
		_previous = DirectGpuRenderingMode.IsEnabled;
		Latch.SetValue(null, value);
	}

	/// <summary>Turns the latch on until the returned scope is disposed.</summary>
	/// <returns>The scope.</returns>
	internal static DirectGpuLatch On() => new(true);

	/// <summary>Turns the latch off until the returned scope is disposed.</summary>
	/// <returns>The scope.</returns>
	internal static DirectGpuLatch Off() => new(false);

	/// <summary>Restores the latch.</summary>
	public void Dispose() => Latch.SetValue(null, _previous);
}
