using System;
using System.Windows;
using CodeBrix.Platform.Foundation.Logging;
using Windows.UI.ViewManagement;

using WpfApplication = System.Windows.Application;
using WpfWindow = System.Windows.Window;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf; //Was previously: Uno.UI.Runtime.Skia.Wpf

internal class WpfApplicationViewExtension : IApplicationViewExtension
{
	private readonly ApplicationView _owner;

	public WpfApplicationViewExtension(object owner)
	{
		_owner = (ApplicationView)owner;
	}

	public bool TryResizeView(Windows.Foundation.Size size)
	{
		if (WpfApplication.Current.MainWindow is not { } wpfMainWindow)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("There is no main window set.");
			}
			return false;
		}

		wpfMainWindow.Width = size.Width;
		wpfMainWindow.Height = size.Height;
		return true;
	}
}
