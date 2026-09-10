#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CodeBrix.Platform.UI.Headless.Tests;

//An application head normally installs the dispatcher overrides at startup; a host-free test
//process has no head, so this installs inert ones. Copied from the CommandBar add-in's suite
//(src/AddIns/Platform.UI.CommandBar.Tests/DispatcherInitializer.cs), namespace changed.
//Reflection is used deliberately: a test-only concern should not widen the framework's internal
//surface with another InternalsVisibleTo grant.

/// <summary>
/// Installs host-free dispatcher overrides so the framework's XAML types can be constructed in a
/// test process with no application head.
/// </summary>
/// <remarks>
/// <para>
/// Every thread reports that it has dispatcher access, and dispatched work runs inline. That is
/// the right semantic for this suite: the ported tests are the ones the original marked
/// <c>[RunsOnUIThread]</c> and nothing more, so "the calling thread IS the UI thread" is exactly
/// the guarantee they were written against - and it is what lets them run on Linux, Windows and
/// macOS identically, since no real UI loop is involved on any of them.
/// </para>
/// <para>
/// What this deliberately does NOT provide is a Window, a XamlRoot, a layout pass driven by a
/// compositor, or rendering. Tests needing any of those stay in
/// src/Platform.UI.RuntimeTests and are not portable here.
/// </para>
/// </remarks>
internal static class DispatcherInitializer
{
	[ModuleInitializer]
	internal static void Initialize()
	{
		var dispatcherType = Type.GetType(
			"CodeBrix.Platform.UI.Dispatching.NativeDispatcher, CodeBrix.Platform.UI.Dispatching");
		if (dispatcherType is null)
		{
			throw new InvalidOperationException(
				"Could not find NativeDispatcher in CodeBrix.Platform.UI.Dispatching. The test "
				+ "project's dispatcher bootstrap needs updating to match the framework.");
		}

		var hasAccessField = dispatcherType.GetField(
			"HasThreadAccessOverride", BindingFlags.NonPublic | BindingFlags.Static);
		var dispatchField = dispatcherType.GetField(
			"DispatchOverride", BindingFlags.NonPublic | BindingFlags.Static);
		if (hasAccessField is null || dispatchField is null)
		{
			throw new InvalidOperationException(
				"NativeDispatcher no longer exposes HasThreadAccessOverride/DispatchOverride. "
				+ "The test project's dispatcher bootstrap needs updating to match the framework.");
		}

		if (hasAccessField.GetValue(null) is null)
		{
			hasAccessField.SetValue(null, (Func<bool>)(static () => true));
		}

		if (dispatchField.GetValue(null) is null)
		{
			// DispatchOverride is Action<Action, NativeDispatcherPriority>; the enum is internal,
			// so the delegate is bound through a generic method instantiated with it.
			var priorityType = dispatchField.FieldType.GetGenericArguments()[1];
			var dispatchMethod = typeof(DispatcherInitializer)
				.GetMethod(nameof(DispatchInline), BindingFlags.NonPublic | BindingFlags.Static)!
				.MakeGenericMethod(priorityType);
			dispatchField.SetValue(null, Delegate.CreateDelegate(dispatchField.FieldType, dispatchMethod));
		}
	}

	private static void DispatchInline<TPriority>(Action action, TPriority priority) => action();
}
