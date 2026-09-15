using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.UI.Controls; //Was previously: Uno.UI.Runtime.Skia.Wpf.UI.Controls

internal class NativeOverlappedPresenter : INativeOverlappedPresenter
{
	private readonly CodeBrixWpfWindow _wpfWindow;
	private bool _isMinimizable = true;
	private bool _isMaximizable = true;
	private bool _isResizable = true;

	// The window wrapper used to be held here only to divide the presenter's size constraints by its
	// rasterization scale; SetSizeConstraints no longer converts, so nothing needs it - see item 8.
	public NativeOverlappedPresenter(CodeBrixWpfWindow wpfWindow)
	{
		_wpfWindow = wpfWindow;
	}

	public OverlappedPresenterState State => _wpfWindow.WindowState switch
	{
		System.Windows.WindowState.Maximized => OverlappedPresenterState.Maximized,
		System.Windows.WindowState.Minimized => OverlappedPresenterState.Minimized,
		System.Windows.WindowState.Normal => OverlappedPresenterState.Restored,
		_ => throw new InvalidOperationException($"Unknown window state: {_wpfWindow.WindowState}")
	};

	public void Maximize() => _wpfWindow.WindowState = System.Windows.WindowState.Maximized;
	public void Minimize(bool activateWindow)
	{
		_wpfWindow.WindowState = System.Windows.WindowState.Minimized;
		if (activateWindow)
		{
			_wpfWindow.Activate();
		}
	}

	public void Restore(bool activateWindow)
	{
		_wpfWindow.WindowState = System.Windows.WindowState.Normal;
		if (activateWindow)
		{
			_wpfWindow.Activate();
		}
	}

	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
	{
		_wpfWindow.WindowStyle = hasBorder ? System.Windows.WindowStyle.SingleBorderWindow : System.Windows.WindowStyle.None;
		// TODO: HasTitleBar support
	}

	public void SetIsAlwaysOnTop(bool isAlwaysOnTop) => _wpfWindow.Topmost = isAlwaysOnTop;

	public void SetIsMaximizable(bool isMaximizable)
	{
		_isMaximizable = isMaximizable;
		SetSizing();
	}

	public void SetIsMinimizable(bool isMinimizable)
	{
		_isMinimizable = isMinimizable;
		SetSizing();
	}

	public void SetIsResizable(bool isResizable)
	{
		_isResizable = isResizable;
		SetSizing();
	}

	public void SetIsModal(bool isModal)
	{
		// TODO: Implement modal
	}

	private void SetSizing()
	{
		if (!_isResizable)
		{
			_wpfWindow.ResizeMode = System.Windows.ResizeMode.NoResize;
		}
		else if (_isMinimizable && !_isMaximizable)
		{
			_wpfWindow.ResizeMode = System.Windows.ResizeMode.CanMinimize;
		}
		else
		{
			_wpfWindow.ResizeMode = System.Windows.ResizeMode.CanResize;
		}
	}

	/// <summary>
	/// Applies the <see cref="OverlappedPresenter"/> size constraints, which are EFFECTIVE PIXELS of
	/// the FRAMED window, to the WPF window's own minimum and maximum.
	/// </summary>
	/// <param name="preferredMinimumWidth">The minimum width in effective pixels, or null for none.</param>
	/// <param name="preferredMinimumHeight">The minimum height in effective pixels, or null for none.</param>
	/// <param name="preferredMaximumWidth">The maximum width in effective pixels, or null for none.</param>
	/// <param name="preferredMaximumHeight">The maximum height in effective pixels, or null for none.</param>
	/// <remarks>
	/// No conversion: WPF's <c>MinWidth</c>, <c>MaxWidth</c> and friends are device-independent units,
	/// which is the same unit the presenter seam speaks, and the same unit this head already assigns
	/// the launch size in. This used to divide by the rasterization scale, which made the head
	/// disagree with itself at any scale other than 1 - see item 8 of the FIXLIST.
	/// </remarks>
	public void SetSizeConstraints(int? preferredMinimumWidth, int? preferredMinimumHeight, int? preferredMaximumWidth, int? preferredMaximumHeight)
	{
		_wpfWindow.MinWidth = preferredMinimumWidth ?? 0;
		_wpfWindow.MinHeight = preferredMinimumHeight ?? 0;
		_wpfWindow.MaxWidth = preferredMaximumWidth ?? double.PositiveInfinity;
		_wpfWindow.MaxHeight = preferredMaximumHeight ?? double.PositiveInfinity;
	}
}
