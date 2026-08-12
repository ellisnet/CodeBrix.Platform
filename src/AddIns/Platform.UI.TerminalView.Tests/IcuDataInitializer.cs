#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CodeBrix.Platform.UI.TerminalView.Tests;

//was previously: copied from src/AddIns/Platform.UI.TextLayout.Tests/IcuDataInitializer.cs (via the
//AdvancedTextEdit suite), with only the namespace changed - this suite also drives the shared text
//engine (CellMetrics measures through TextLayoutEngine) with no application head.

/// <summary>
/// Stands in for the ICU module initializer that an application head normally generates, so the
/// text engine can start on Windows and macOS with no head present.
/// </summary>
/// <remarks>
/// <para>
/// On Linux this is a no-op: ICU comes from the dynamic linker search path, needs no data file, and
/// the engine's own <c>EnsureInitialized</c> lazily loads it on the first layout call. Nothing about
/// the Linux run changes because of this type.
/// </para>
/// <para>
/// On Windows and macOS ICU has no system copy to find, and its data has to be handed in explicitly
/// from a managed resource. The engine's lazy path guesses the ENTRY assembly for that resource,
/// which is right for a head but wrong here - under a test host the entry assembly is the runner,
/// not this test assembly, and this test assembly is the one carrying icudt.dat. So name it
/// directly, before any test runs.
/// </para>
/// <para>
/// The engine's ICU type is private and deliberately not part of any public surface; reflection is
/// how the generated head initializer reaches it too, and matching that keeps this a test-only
/// concern rather than a reason to widen the engine's API.
/// </para>
/// </remarks>
internal static class IcuDataInitializer
{
	[ModuleInitializer]
	internal static void Initialize()
	{
		// Matched by $(CodeBrixTestsNeedIcuData) in the csproj: exactly the platforms where the
		// build embeds icudt.dat is where the engine has to be told about it.
		if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
		{
			return;
		}

		var icuType = Type.GetType("Microsoft.UI.Xaml.Documents.UnicodeText+ICU, CodeBrix.Platform.UI");
		var setDataAssembly = icuType?.GetMethod(
			"SetDataAssembly",
			BindingFlags.Public | BindingFlags.Static);

		if (setDataAssembly is null)
		{
			// Fail loudly and early rather than letting every test fail later with a native loader
			// error that says nothing about the real cause.
			throw new InvalidOperationException(
				"Could not find Microsoft.UI.Xaml.Documents.UnicodeText+ICU.SetDataAssembly in "
				+ "CodeBrix.Platform.UI. The test project's ICU bootstrap needs updating to match "
				+ "the engine.");
		}

		setDataAssembly.Invoke(null, [typeof(IcuDataInitializer).Assembly]);
	}
}
