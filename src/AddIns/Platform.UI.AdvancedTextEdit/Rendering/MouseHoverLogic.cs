#nullable enable

using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/MouseHoverLogic.cs in the AvalonEdit repo
//(MIT). Re-expressed on this framework's input stack: MouseEnter/MouseMove/MouseLeave became
//PointerEntered/PointerMoved/PointerExited, the hover event args type is
//PointerRoutedEventArgs, the timer is Microsoft.UI.Xaml.DispatcherTimer, and the WPF Vector
//distance check became plain double math. The upstream SystemParameters hover thresholds are
//constants here (4 device-independent pixels, 400 ms - the WPF defaults) because this framework
//exposes no equivalent system settings.

/// <summary>
/// Encapsulates and adds mouse-hover support to UI elements.
/// </summary>
public class MouseHoverLogic : IDisposable
{
	// Hover thresholds; these are the default values of the system parameters the
	// upstream implementation read.
	const double MouseHoverWidth = 4.0;
	const double MouseHoverHeight = 4.0;
	static readonly TimeSpan MouseHoverTime = TimeSpan.FromMilliseconds(400);

	readonly UIElement target;

	DispatcherTimer? mouseHoverTimer;
	Point mouseHoverStartPoint;
	PointerRoutedEventArgs? mouseHoverLastEventArgs;
	bool mouseHovering;

	/// <summary>
	/// Creates a new instance and attaches itself to the <paramref name="target" /> element.
	/// </summary>
	public MouseHoverLogic(UIElement target)
	{
		if (target == null)
			throw new ArgumentNullException(nameof(target));
		this.target = target;
		this.target.PointerExited += MouseHoverLogicPointerExited;
		this.target.PointerMoved += MouseHoverLogicPointerMoved;
		this.target.PointerEntered += MouseHoverLogicPointerEntered;
	}

	void MouseHoverLogicPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		Point position = e.GetCurrentPoint(this.target).Position;
		if (Math.Abs(mouseHoverStartPoint.X - position.X) > MouseHoverWidth
			|| Math.Abs(mouseHoverStartPoint.Y - position.Y) > MouseHoverHeight)
		{
			StartHovering(e);
		}
		// do not set e.Handled - allow others to also handle PointerMoved
	}

	void MouseHoverLogicPointerEntered(object sender, PointerRoutedEventArgs e)
	{
		StartHovering(e);
		// do not set e.Handled - allow others to also handle PointerEntered
	}

	void StartHovering(PointerRoutedEventArgs e)
	{
		StopHovering();
		mouseHoverStartPoint = e.GetCurrentPoint(this.target).Position;
		mouseHoverLastEventArgs = e;
		mouseHoverTimer = new DispatcherTimer { Interval = MouseHoverTime };
		mouseHoverTimer.Tick += OnMouseHoverTimerElapsed;
		mouseHoverTimer.Start();
	}

	void MouseHoverLogicPointerExited(object sender, PointerRoutedEventArgs e)
	{
		StopHovering();
		// do not set e.Handled - allow others to also handle PointerExited
	}

	void StopHovering()
	{
		if (mouseHoverTimer != null)
		{
			mouseHoverTimer.Stop();
			mouseHoverTimer.Tick -= OnMouseHoverTimerElapsed;
			mouseHoverTimer = null;
		}
		if (mouseHovering)
		{
			mouseHovering = false;
			Debug.Assert(mouseHoverLastEventArgs != null);
			OnMouseHoverStopped(mouseHoverLastEventArgs);
		}
	}

	void OnMouseHoverTimerElapsed(object? sender, object e)
	{
		if (mouseHoverTimer != null)
		{
			mouseHoverTimer.Stop();
			mouseHoverTimer.Tick -= OnMouseHoverTimerElapsed;
			mouseHoverTimer = null;
		}

		mouseHovering = true;
		Debug.Assert(mouseHoverLastEventArgs != null);
		OnMouseHover(mouseHoverLastEventArgs);
	}

	/// <summary>
	/// Occurs when the mouse starts hovering over a certain location.
	/// </summary>
	public event EventHandler<PointerRoutedEventArgs>? MouseHover;

	/// <summary>
	/// Raises the <see cref="MouseHover"/> event.
	/// </summary>
	protected virtual void OnMouseHover(PointerRoutedEventArgs e)
	{
		if (MouseHover != null)
		{
			MouseHover(this, e);
		}
	}

	/// <summary>
	/// Occurs when the mouse stops hovering over a certain location.
	/// </summary>
	public event EventHandler<PointerRoutedEventArgs>? MouseHoverStopped;

	/// <summary>
	/// Raises the <see cref="MouseHoverStopped"/> event.
	/// </summary>
	protected virtual void OnMouseHoverStopped(PointerRoutedEventArgs e)
	{
		if (MouseHoverStopped != null)
		{
			MouseHoverStopped(this, e);
		}
	}

	bool disposed;

	/// <summary>
	/// Removes the hover support from the target element.
	/// </summary>
	public void Dispose()
	{
		if (!disposed)
		{
			this.target.PointerExited -= MouseHoverLogicPointerExited;
			this.target.PointerMoved -= MouseHoverLogicPointerMoved;
			this.target.PointerEntered -= MouseHoverLogicPointerEntered;
		}
		disposed = true;
	}
}
