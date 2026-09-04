#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

//was previously: not needed upstream - SkiaSharp's own view tests ran inside a real application.
//Here, an application head normally installs the dispatcher overrides at startup; a host-free
//test process has no head, so this installs inert ones. Copied from the AdvancedTextEdit add-in's
//suite (src/AddIns/Platform.UI.AdvancedTextEdit.Tests/DispatcherInitializer.cs), namespace changed.
//Reflection is used deliberately: a test-only concern should not widen the framework's internal
//surface with another InternalsVisibleTo grant.

/// <summary>
/// Installs host-free dispatcher overrides so the add-in's XAML-derived controls
/// (<c>SKXamlCanvas</c> and its dependency objects) can be constructed in a test process with no
/// application head.
/// </summary>
/// <remarks>
/// <para>
/// Every thread reports that it has dispatcher access, and dispatched work runs inline. That is
/// the right semantic for unit tests, which are synchronous and assert on immediate effects - and
/// it is what makes <c>SKXamlCanvas.Invalidate()</c> paint before the call returns, since the
/// control's Invalidate dispatches when it does not have thread access.
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
