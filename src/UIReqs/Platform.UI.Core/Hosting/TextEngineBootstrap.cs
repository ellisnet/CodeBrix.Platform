using System;
using System.Reflection;

namespace CodeBrix.Platform.UI.Core.UIReqs.Hosting;

/// <summary>
/// Loads the native library the text engine lays a string out with.
/// </summary>
/// <remarks>
/// An application head gets this from a module initializer its build generates; a project that
/// uses the framework WITHOUT being a head - this one, and the two host-free add-in suites in
/// this repository - has to make the call itself. Without it, measuring a TextBlock throws deep
/// inside the bidirectional pass, as ArgumentNullException(Parameter 'handle') out of
/// NativeLibrary.TryGetExport, and a scenario that asked "is there ink?" would be answered by an
/// exception rather than by the panel.
/// </remarks>
public static class TextEngineBootstrap
{
	private const string UnicodeTextTypeName = "Microsoft.UI.Xaml.Documents.UnicodeText, CodeBrix.Platform.UI";
	private const string InitializeMethodName = "EnsureEngineInitialized";

	private static bool _initialized;

	/// <summary>Loads the text engine once per process.</summary>
	/// <exception cref="InvalidOperationException">The framework no longer offers the entry point.</exception>
	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		var unicodeText = Type.GetType(UnicodeTextTypeName);
		var initialize = unicodeText?.GetMethod(
			InitializeMethodName,
			BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

		if (initialize is null)
		{
			throw new InvalidOperationException(
				$"Could not find UnicodeText.{InitializeMethodName} in CodeBrix.Platform.UI. The "
				+ "harness's text-engine bootstrap needs updating to match the framework.");
		}

		initialize.Invoke(null, null);
		_initialized = true;
	}
}
