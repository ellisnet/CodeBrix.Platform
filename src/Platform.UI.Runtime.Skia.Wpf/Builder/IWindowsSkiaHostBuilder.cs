#nullable enable

using System;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public interface IWindowsSkiaHostBuilder
{
	internal Func<System.Windows.Application>? WpfApplication { get; set; }

	/// <summary>
	/// The WPF dispatcher priority tier the CodeBrix dispatcher pump runs at. Defaults to
	/// <see cref="WpfDispatcherScheduling.RenderFirst"/>; continuously-repainting apps should opt
	/// into <see cref="WpfDispatcherScheduling.InputFair"/> so their own UI work cannot starve
	/// keyboard and pointer input.
	/// </summary>
	internal WpfDispatcherScheduling DispatcherScheduling { get; set; }
}
