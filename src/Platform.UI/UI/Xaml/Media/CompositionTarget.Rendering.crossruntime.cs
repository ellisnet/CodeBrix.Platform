#if !__NETSTD_REFERENCE__
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Composition;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Helpers;
using CodeBrix.Platform.UI.Hosting;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private static readonly long _start = Stopwatch.GetTimestamp();
	// We're using this table as a set with weakref keys. values are always null
	private static readonly ConditionalWeakTable<CompositionTarget, object> _targets = new();
	private static bool _isRenderingActive;

	private readonly Lock _xamlRootBoundsGate = new();

	// only set and read under _xamlRootBoundsGate
	private Size _xamlRootBounds;
	// only set and read under _xamlRootBoundsGate
	private float _xamlRootRasterizationScale;

	// set once, in the constructor
	private readonly ICompositionTargetPlatform? _platform;

	/// <summary>
	/// Gets the platform side of this target (see <see cref="ICompositionTargetPlatform"/>).
	/// </summary>
	internal ICompositionTargetPlatform Platform => _platform!;

	internal event Action? FrameRendered;

	private static event EventHandler<object>? _rendering;

	public static event EventHandler<object>? Rendering
	{
		add
		{
			NativeDispatcher.CheckThreadAccess();
			_rendering += value;
			if (!_isRenderingActive)
			{
				_isRenderingActive = true;
				foreach (var (target, _) in _targets)
				{
					((ICompositionTarget)target).RequestNewFrame();
				}
			}
		}
		remove
		{
			NativeDispatcher.CheckThreadAccess();
			_rendering -= value;
			if (_rendering == null)
			{
				_isRenderingActive = false;
			}
		}
	}

	/// <summary>
	/// Reads the size and rasterization scale of the XAML root, as last reported to this target. Safe to call from
	/// the rendering thread.
	/// </summary>
	/// <param name="xamlRootBounds">The size of the XAML root, in logical pixels.</param>
	/// <param name="rasterizationScale">The rasterization scale of the XAML root.</param>
	internal void GetXamlRootBounds(out Size xamlRootBounds, out float rasterizationScale)
	{
		lock (_xamlRootBoundsGate)
		{
			xamlRootBounds = _xamlRootBounds;
			rasterizationScale = _xamlRootRasterizationScale;
		}
	}

	private void Render()
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Render)} begins with timestamp {Stopwatch.GetTimestamp()}");

		NativeDispatcher.CheckThreadAccess();

		var rootElement = ContentRoot.VisualTree.RootElement;

		_platform!.RecordFrame();

		if (_isRenderingActive)
		{
			((ICompositionTarget)this).RequestNewFrame();
		}

		if (rootElement.XamlRoot is not null)
		{
			XamlRootMap.GetHostForRoot(rootElement.XamlRoot)?.InvalidateRender();
		}

		_platform.UpdateNativeElementsOrder();

		FrameRendered?.Invoke();
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Render)} ends");
	}

	internal static void InvokeRendering()
	{
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			_rendering?.Invoke(null, new RenderingEventArgs(Stopwatch.GetElapsedTime(_start)));
		}
		else
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				_rendering?.Invoke(null, new RenderingEventArgs(Stopwatch.GetElapsedTime(_start)));
			}, NativeDispatcherPriority.High);
		}
	}
}
#endif
