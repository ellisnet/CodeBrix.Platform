using System;
using System.Diagnostics;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Hosting;
using Timer = System.Timers.Timer;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.X11; //Was previously: Uno.WinUI.Runtime.Skia.X11

internal partial class X11XamlRootHost
{
	private readonly Timer _renderTimer;

	private Timer CreateRenderTimer()
	{
		var timer = new Timer { AutoReset = false, Interval = TimeSpan.FromSeconds(1.0 / FeatureConfiguration.CompositionTarget.FrameRate).TotalMilliseconds };
		timer.Elapsed += (_, _) => _renderer?.Render();
		return timer;
	}

	internal void UpdateRenderTimerFps(double fps)
	{
		if (FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate)
		{
			_renderTimer.Interval = TimeSpan.FromSeconds(1.0 / fps).TotalMilliseconds;
		}
	}

	void IXamlRootHost.InvalidateRender()
	{
		if (!_closed.Task.IsCompleted)
		{
			_renderTimer.Enabled = true;
		}
	}
}
