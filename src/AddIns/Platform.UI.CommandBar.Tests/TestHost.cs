using System;
using System.Reflection;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The two things an application head would have done before a control is measured, done here
/// instead: register the add-in's default styles, and load the text engine's native library.
/// </summary>
/// <remarks>
/// <para>
/// Only the tests that MEASURE need this. Everything else in the suite - command binding, tooltip
/// composition, the drop-down's state machine - is about properties rather than pixels, and runs
/// against a bare control exactly as the scaffold's smoke tests do. Keeping the two apart means a
/// failure in the measuring tests points at layout rather than at the harness.
/// </para>
/// <para>
/// Reflection is used for the text engine deliberately, following the reasoning already recorded
/// in DispatcherInitializer: a test-only concern should not widen the framework's internal surface
/// with another grant.
/// </para>
/// </remarks>
internal static class TestHost
{
	/// <summary>Serialises the one-time preparation, so a second caller waits for it.</summary>
	private static readonly object ReadyLock = new();

	/// <summary>Whether the process has already been prepared.</summary>
	private static volatile bool _ready;

	/// <summary>
	/// Prepares the process to measure a templated control, once.
	/// </summary>
	/// <remarks>
	/// The flag is set AFTER the work, under a lock, and that order is the whole point: setting it
	/// first lets a second thread return from here while the first is still half way through
	/// registering the default styles, and a control built in that window gets
	/// "ResourceDictionary was registered as style provider for ToolBarOverflowButton but doesn't
	/// contain matching style". The lock alone is not enough either - a reader that sees a
	/// half-written flag is exactly the same race - so the flag is volatile and is written last.
	/// <para>
	/// This makes the bootstrap safe to CALL from several threads; it does not make the framework's
	/// object model thread-safe. Test classes that build XAML objects still have to be serialised -
	/// see the "the host-free bootstrap" section of the add-in's AGENT-README.txt.
	/// </para>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// The framework no longer exposes the text engine's initialization entry point, so the
	/// bootstrap here needs updating to match it.
	/// </exception>
	internal static void EnsureReady()
	{
		if (_ready)
		{
			return;
		}

		lock (ReadyLock)
		{
			if (_ready)
			{
				return;
			}

			RegisterDefaultStyles();
			InitializeTextEngine();

			_ready = true;
		}
	}

	/// <summary>
	/// Brings the add-in's default styles into a process that has no application of its own.
	/// </summary>
	/// <remarks>
	/// <para>
	/// In an application the XAML source generator writes the application's own
	/// <c>GlobalStaticResources</c>, whose <c>Initialize</c> calls <c>RegisterDefaultStyles</c> on
	/// every referenced assembly that has any - which is how a <see cref="ToolButton"/> in a real
	/// application finds the template in Themes/Generic.xaml. A test project has no XAML of its own,
	/// so nothing generates that call, and a control constructed here would have no template at all.
	/// Calling the add-in's own generated entry points is exactly what the missing application code
	/// would do, so a test measures the template that really ships.
	/// </para>
	/// <para>
	/// <see cref="DefaultStyleInitializer"/> already made those two calls from a module initializer,
	/// before any test ran, which is what lets a test that never asks for this host measure a
	/// templated bar. This method therefore adds only the third call, which that initializer does
	/// not make; the delegation keeps ONE description of how a host-free process gets its styles.
	/// </para>
	/// </remarks>
	private static void RegisterDefaultStyles()
	{
		DefaultStyleInitializer.Initialize();

		GlobalStaticResources.RegisterResourceDictionariesBySource();
	}

	/// <summary>
	/// Loads the native ICU library the text engine needs to lay a string out.
	/// </summary>
	/// <remarks>
	/// An application head loads it from a generated module initializer. Without it, measuring a
	/// TextBlock throws deep inside the bidi pass - measured as
	/// ArgumentNullException(Parameter 'handle') from NativeLibrary.TryGetExport - and the framework
	/// swallows it, so the text silently measures as nothing. That would make every "does the label
	/// take space?" test pass for the wrong reason.
	/// </remarks>
	private static void InitializeTextEngine()
	{
		var unicodeText = Type.GetType("Microsoft.UI.Xaml.Documents.UnicodeText, CodeBrix.Platform.UI");
		var initialize = unicodeText?.GetMethod(
			"EnsureEngineInitialized",
			BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

		if (initialize is null)
		{
			throw new InvalidOperationException(
				"Could not find UnicodeText.EnsureEngineInitialized in CodeBrix.Platform.UI. The "
				+ "test project's text-engine bootstrap needs updating to match the framework.");
		}

		initialize.Invoke(null, null);
	}
}
