#nullable enable

using System;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Composition.Contracts;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Dispatching.Contracts;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// A minimal non-Skia platform for the Core assemblies: the implementations of the platform contracts that the
/// host-free path (creating elements, applying styles, resolving bindings, navigating a frame) reaches, registered
/// the way a platform library registers its own (CodeBrix.Platform's Skia bootstrap, later CodeBrix.Android's and
/// CodeBrix.Mobile's). Nothing here draws: the tests exercise the platform-neutral logic only.
/// </summary>
internal static class TestPlatform
{
	private static readonly object _gate = new();
	private static bool _registered;

#pragma warning disable CA2255 // A test process's platform must exist before the first framework object is created.
	[ModuleInitializer]
#pragma warning restore CA2255
	internal static void Initialize() => EnsureRegistered();

	/// <summary>Registers the test platform once.</summary>
	internal static void EnsureRegistered()
	{
		lock (_gate)
		{
			if (_registered)
			{
				return;
			}

			var pump = new InlineDispatcherPump();
			ApiExtensibility.Register(typeof(IDispatcherPumpPlatform), _ => pump);

			var application = new NullApplicationPlatform();
			ApiExtensibility.Register(typeof(IApplicationPlatform), _ => application);

			var text = new NullTextPlatform();
			ApiExtensibility.Register(typeof(ITextPlatform), _ => text);

			var composition = new NullCompositionPlatform();
			ApiExtensibility.Register(typeof(ICompositionPlatform), _ => composition);
			ApiExtensibility.Register(typeof(IRenderingPlatform), _ => new NullRenderingPlatform());
			ApiExtensibility.Register(typeof(IFocusPlatform), _ => new NullFocusPlatform());

			// Controls that load localized strings (TabView, for one) reach ApplicationData through the resource loader.
			var applicationData = new TemporaryApplicationDataPlatform();
			ApiExtensibility.Register(typeof(CodeBrix.Platform.Contracts.IApplicationDataPlatform), _ => applicationData);

			// The device form a platform reports (WPE1-11, the live DeviceFamily fences). It starts Unknown - what
			// AnalyticsInfo.DeviceForm is with no extension at all - so every other test sees the value it saw before.
			ApiExtensibility.Register(typeof(Windows.System.Profile.Internal.IAnalyticsInfoExtension), _ => DeviceForm);

			// The user's preferred languages. ApplicationLanguages reads them only on Windows (elsewhere it uses the
			// manifest languages alone), so controls that load localized strings or create a Calendar reach this contract
			// only on a Windows host. It reports none, so Windows falls back to the manifest languages like every other OS.
			var globalizationPreferences = new NoGlobalizationPreferencesPlatform();
			ApiExtensibility.Register(typeof(CodeBrix.Platform.Contracts.IGlobalizationPreferencesPlatform), _ => globalizationPreferences);

			_registered = true;
		}
	}

	/// <summary>The device form this test platform reports; a test may change it and must put it back.</summary>
	internal static TestAnalyticsInfo DeviceForm { get; } = new();

	/// <summary>A platform's device form (AnalyticsInfo.DeviceForm), settable by a test.</summary>
	internal sealed class TestAnalyticsInfo : Windows.System.Profile.Internal.IAnalyticsInfoExtension
	{
		/// <summary>The form reported now.</summary>
		public Windows.System.Profile.Internal.CodeBrixDeviceForm Form { get; set; } = Windows.System.Profile.Internal.CodeBrixDeviceForm.Unknown;

		/// <inheritdoc />
		public Windows.System.Profile.Internal.CodeBrixDeviceForm GetDeviceForm() => Form;
	}

	/// <summary>Application data folders under a per-process temporary folder (nothing is written by the tests).</summary>
	private sealed class TemporaryApplicationDataPlatform : CodeBrix.Platform.Contracts.IApplicationDataPlatform
	{
		private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CodeBrix.Platform.UI.Core.Tests", Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));

		public string GetLocalFolderPath() => System.IO.Path.Combine(_root, "Local");

		public string GetRoamingFolderPath() => System.IO.Path.Combine(_root, "Roaming");

		public string GetLocalCacheFolderPath() => System.IO.Path.Combine(_root, "LocalCache");

		public string GetTemporaryFolderPath() => System.IO.Path.Combine(_root, "Temp");

		public string GetSettingsFolderPath() => System.IO.Path.Combine(_root, "Settings");
	}

	/// <summary>An operating system that reports no preferred languages.</summary>
	private sealed class NoGlobalizationPreferencesPlatform : CodeBrix.Platform.Contracts.IGlobalizationPreferencesPlatform
	{
		public System.Collections.Generic.IReadOnlyList<string> Languages { get; } = [];
	}

	/// <summary>The application-level platform: it has no extensions of its own to register.</summary>
	private sealed class NullApplicationPlatform : IApplicationPlatform
	{
		public void RegisterExtensions() { }
	}

	/// <summary>Text measures as nothing: the tests check bindings and styles, not text layout.</summary>
	private sealed class NullTextPlatform : ITextPlatform
	{
		public ITextLayout EmptyLayout { get; } = new NullTextLayout();

		public ITextLayout CreateLayout(TextBlock textBlock, Size availableSize, out Size desiredSize)
		{
			desiredSize = default;
			return EmptyLayout;
		}

		// A platform whose native text field edits (IsManagedEditing false); the element handler tests drive it through
		// the text boxes' raise entry points.
		public ITextBoxPlatform CreateTextBoxPlatform(TextBox textBox) => new FakeTextBoxPlatform();
	}

	private sealed class NullTextLayout : ITextLayout
	{
		public Rect GetRectForIndex(int adjustedIndex) => default;

		public int GetIndexAt(Point p, bool ignoreEndingNewLine, bool extendedSelection) => 0;

		public Hyperlink? GetHyperlinkAt(Point point) => null;

		public (int start, int length) GetWordAt(int index, bool right) => (0, 0);

		public (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index) => (0, 0, true, true, 0);

		public bool IsBaseDirectionRightToLeft => false;
	}

	/// <summary>Every thread has dispatcher access and dispatched work runs inline.</summary>
	private sealed class InlineDispatcherPump : IDispatcherPumpPlatform
	{
		public bool HasThreadAccess => true;

		public void Schedule(Action dispatchCallback, NativeDispatcherPriority priority) => dispatchCallback();
	}

	/// <summary>Composition objects get platform state that paints nothing and hits nothing.</summary>
	private sealed class NullCompositionPlatform : ICompositionPlatform
	{
		public IVisualPlatform CreateVisualPlatform(Visual owner) => NullVisualPlatform.Instance;

		public ICompositionBrushPlatform CreateBrushPlatform(CompositionBrush owner) => new NullBrushPlatform();

		public ICompositionClipPlatform CreateClipPlatform(CompositionClip owner) => NullClipPlatform.Instance;

		public ICompositionShapePlatform CreateShapePlatform(CompositionShape owner) => NullShapePlatform.Instance;

		public ICompositionSurfacePlatform CreateSurfacePlatform(PlatformCompositionSurface owner) => throw new NotSupportedException("The host-free test platform has no surfaces.");

		public bool AreEffectsSupported(Compositor? compositor) => false;

		public bool AreEffectsFast(Compositor? compositor) => false;
	}

	private sealed class NullVisualPlatform : IVisualPlatform
	{
		internal static readonly NullVisualPlatform Instance = new();

		public void DiscardPaintCache() { }

		public void DiscardChildrenCache() { }

		public bool CanPaint() => false;

		public bool RequiresRepaintOnEveryFrame => false;

		public bool HitTest(Point point) => false;
	}

	private sealed class NullBrushPlatform : ICompositionBrushPlatform
	{
		public bool CanPaint() => false;

		public bool RequiresRepaintOnEveryFrame => false;

		public System.Numerics.Vector2? Size => null;

		public void OnPropertyChanged(string? propertyName) { }

		public void Dispose() { }
	}

	private sealed class NullClipPlatform : ICompositionClipPlatform
	{
		internal static readonly NullClipPlatform Instance = new();
	}

	/// <summary>Allows a real Core content root without a window or a renderer.</summary>
	private sealed class NullRenderingPlatform : IRenderingPlatform, ICompositionTargetPlatform
	{
		public void OnRenderFrameOpportunity(Microsoft.UI.Xaml.XamlRoot xamlRoot) { }
		public ICompositionTargetPlatform CreateCompositionTargetPlatform(Microsoft.UI.Xaml.Media.CompositionTarget target) => this;
		public bool CanRecordFrame() => false;
		public void RecordFrame() { }
		public void UpdateNativeElementsOrder() { }
	}

	/// <summary>Keeps focus in Core; there is no native view to focus.</summary>
	private sealed class NullFocusPlatform : IFocusPlatform
	{
		public void FocusNative(Microsoft.UI.Xaml.UIElement? element) { }
	}

	private sealed class NullShapePlatform : ICompositionShapePlatform
	{
		internal static readonly NullShapePlatform Instance = new();

		public bool CanPaint() => false;

		public bool HitTest(Point point) => false;

		public void OnGeometryChanged() { }
	}
}
