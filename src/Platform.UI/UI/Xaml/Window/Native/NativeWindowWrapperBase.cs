#nullable enable

using System;
using Microsoft.UI.Content;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Core;

#if HAS_CODEBRIX_WINUI
using Microsoft.UI.Dispatching;
#else
using Windows.System;
#endif

namespace CodeBrix.Platform.UI.Xaml.Controls; //Was previously: Uno.UI.Xaml.Controls

internal abstract class NativeWindowWrapperBase : INativeWindowWrapper
{
	public const int InitialWidth = 1024;
	public const int InitialHeight = 640;

	protected readonly ContentSite _contentSite = new();
	private Rect _bounds;
	private Rect _visibleBounds;
	private bool _visible;
	private PointInt32 _position;
	private string _title = "";
	private CoreWindowActivationState _activationState;
	private XamlRoot? _xamlRoot;
	private protected Window? _window;
	private float _rasterizationScale;
	private readonly SerialDisposable _presenterSubscription = new SerialDisposable();

	protected NativeWindowWrapperBase(Window window, XamlRoot xamlRoot) : this()
	{
		SetWindow(window, xamlRoot);
	}

	protected NativeWindowWrapperBase()
	{
	}

	public ContentSiteView ContentSiteView => _contentSite.View;

	internal protected XamlRoot? XamlRoot => _xamlRoot;

	internal protected Window? Window => _window;

	internal bool AssociatedWithManagedWindow => _window != null && _xamlRoot != null;

	public bool WasShown { get; set; }

	internal void SetWindow(Window window, XamlRoot xamlRoot)
	{
		_window = window;
		_xamlRoot = xamlRoot;

		// Relay the WinUI title-bar mode to the platform head (which hides/shows the native
		// decorations accordingly). Without this relay the per-head ExtendContentIntoTitleBar
		// overrides are never reached. Wrapper and window lifetimes match, so the
		// subscription needs no teardown.
		window.AppWindow.TitleBar.ExtendsContentIntoTitleBarChanged += ExtendContentIntoTitleBar;
		if (window.AppWindow.TitleBar.ExtendsContentIntoTitleBar)
		{
			ExtendContentIntoTitleBar(true);
		}
	}

	public abstract object? NativeWindow { get; }

	public Rect Bounds
	{
		get => _bounds;
		set
		{
			if (_bounds != value)
			{
				_bounds = value;
				SizeChanged?.Invoke(this, value.Size);

				RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.ActualSizeChange);
			}
		}
	}

	public Rect VisibleBounds
	{
		get => _visibleBounds;
		set
		{
			if (_visibleBounds != value)
			{
				_visibleBounds = value;
				VisibleBoundsChanged?.Invoke(this, value);
			}
		}
	}

	/// <summary>
	/// The same as setting <see cref="VisibleBounds"/>, <see cref="Bounds"/> and <see cref="Size"/> but makes sure the
	/// fired events are fired only after both properties are updated "atomically"
	/// </summary>
	public void SetBoundsAndVisibleBounds(Rect bounds, Rect visibleBounds)
	{
		if (_visibleBounds != visibleBounds)
		{
			_visibleBounds = visibleBounds;
			VisibleBoundsChanged?.Invoke(this, visibleBounds);
		}

		if (_bounds != bounds)
		{
			_bounds = bounds;
			SizeChanged?.Invoke(this, bounds.Size);
			RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.ActualSizeChange);
		}
	}

	public CoreWindowActivationState ActivationState
	{
		get => _activationState;
		set
		{
			if (_activationState != value)
			{
				_activationState = value;
				ActivationChanged?.Invoke(this, value);
			}
		}
	}

	public bool IsVisible
	{
		get => _visible;
		set
		{
			if (_visible != value)
			{
				_visible = value;
				VisibilityChanged?.Invoke(this, value);

				_contentSite.IsSiteVisible = value;
				RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.SiteVisibleChange);
			}
		}
	}

	public float RasterizationScale
	{
		get => _rasterizationScale;
		set
		{
			if (_rasterizationScale != value)
			{
				_rasterizationScale = value;

				_contentSite.ParentScale = value;
				RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.RasterizationScaleChange);
			}
		}
	}

	public virtual string Title
	{
		get => _title;
		set
		{
			_title = value;
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"Setting the title of the window is not supported on this platform yet");
			}
		}
	}

	public PointInt32 Position
	{
		get => _position;
		set
		{
			if (!_position.Equals(value))
			{
				_position = value;
				_window?.AppWindow.OnAppWindowChanged(new AppWindowChangedEventArgs() { DidPositionChange = true });
			}
		}
	}

	public SizeInt32 Size { get; private set; }

	public SizeInt32 ClientSize { get; private set; }

	/// <summary>
	/// True once <see cref="Size"/> (which includes whatever non-client frame the windowing system
	/// draws) is known to be larger than <see cref="ClientSize"/> in either dimension - that is, once
	/// the frame extents are knowable. It is false while the two are equal, which is the case for an
	/// undecorated window and for a window the window manager has not framed yet.
	/// </summary>
	internal static bool HasNonClientFrame(SizeInt32 framedSize, SizeInt32 clientSize)
		=> framedSize.Width > clientSize.Width || framedSize.Height > clientSize.Height;

	/// <summary>
	/// Converts a size expressed the way <see cref="Size"/> and <see cref="Resize"/> express it - in
	/// screen coordinates, including the non-client frame - into the size the window's own client area
	/// has to be given to produce it, using the frame extents implied by
	/// <paramref name="framedSize"/> and <paramref name="clientSize"/>. Each dimension is clamped to at
	/// least one pixel. When no frame is known the requested size is returned unchanged.
	/// </summary>
	/// <param name="requestedFramedSize">The framed size the caller asked for.</param>
	/// <param name="framedSize">The window's current framed size, i.e. <see cref="Size"/>.</param>
	/// <param name="clientSize">The window's current client size, i.e. <see cref="ClientSize"/>.</param>
	/// <returns>The size to give the client area.</returns>
	internal static SizeInt32 ToClientSize(SizeInt32 requestedFramedSize, SizeInt32 framedSize, SizeInt32 clientSize)
	{
		var frameWidth = Math.Max(0, framedSize.Width - clientSize.Width);
		var frameHeight = Math.Max(0, framedSize.Height - clientSize.Height);

		return new SizeInt32
		{
			Width = Math.Max(1, requestedFramedSize.Width - frameWidth),
			Height = Math.Max(1, requestedFramedSize.Height - frameHeight),
		};
	}

	/// <summary>
	/// <see cref="ToClientSize(SizeInt32, SizeInt32, SizeInt32)"/> against the wrapper's current
	/// <see cref="Size"/> and <see cref="ClientSize"/>.
	/// </summary>
	/// <param name="requestedFramedSize">The framed size the caller asked for.</param>
	/// <returns>The size to give the client area.</returns>
	internal SizeInt32 ToClientSize(SizeInt32 requestedFramedSize)
		=> ToClientSize(requestedFramedSize, Size, ClientSize);

	protected void SetSizes(SizeInt32 size, SizeInt32 clientSize)
	{
		var anySizeChanged = false;

		if (!Size.Equals(size))
		{
			Size = size;
			anySizeChanged = true;
		}

		if (!ClientSize.Equals(clientSize))
		{
			ClientSize = clientSize;
			anySizeChanged = true;
		}

		if (anySizeChanged)
		{
			_window?.AppWindow.OnAppWindowChanged(new AppWindowChangedEventArgs() { DidSizeChange = true });
		}
	}

	public DispatcherQueue DispatcherQueue => throw new NotImplementedException();

	public event EventHandler<Size>? SizeChanged;
	public event EventHandler<Rect>? VisibleBoundsChanged;
	public event EventHandler<CoreWindowActivationState>? ActivationChanged;
	public event EventHandler<bool>? VisibilityChanged;
	public event EventHandler<AppWindowClosingEventArgs>? Closing;
	public event EventHandler? Shown;

	internal protected virtual void Activate()
	{
	}

	/// <summary>
	/// Request the close of the native window
	/// </summary>
	protected virtual void CloseCore()
	{
	}

	public void Close()
	{
		CloseCore();

		IsVisible = false;
	}

	public virtual void ExtendContentIntoTitleBar(bool extend) { }

	public virtual void Show(bool activateWindow)
	{
		if (!WasShown)
		{
			WasShown = true;
			ShowCore();

			// On single-window targets, the window is already shown with splash screen
			// so we must ensure the property is initialized correctly.
			IsVisible = true;
			Shown?.Invoke(this, EventArgs.Empty);
		}

		if (activateWindow)
		{
			Activate();
		}
	}

	protected virtual void ShowCore() { }

	protected AppWindowClosingEventArgs RaiseClosing()
	{
		var args = new AppWindowClosingEventArgs();
		Closing?.Invoke(this, args);
		return args;
	}

	public void SetPresenter(AppWindowPresenter presenter)
	{
		_presenterSubscription.Disposable?.Dispose();
		switch (presenter)
		{
			case FullScreenPresenter _:
				_presenterSubscription.Disposable = ApplyFullScreenPresenter();
				break;
			case OverlappedPresenter overlapped:
				_presenterSubscription.Disposable = ApplyOverlappedPresenter(overlapped);
				break;
			default:
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning($"AppWindow presenter type {presenter.GetType()} is not supported yet");
				}
				break;
		}
	}

	protected virtual IDisposable ApplyFullScreenPresenter() => Disposable.Empty;

	protected virtual IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter) => Disposable.Empty;

	private void RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs args)
	{
		XamlRoot?.VisualTree.ContentRoot.CompositionContent?.RaiseStateChanged(args);
	}
	public virtual void Move(PointInt32 position)
	{
	}

	public virtual void Resize(SizeInt32 size)
	{
	}

	public void Destroy() { }

	public void Hide() => IsVisible = false;

	public virtual void SetIcon(string iconPath)
	{
	}

#if false
	public abstract Size GetWindowSize();
#endif
}
