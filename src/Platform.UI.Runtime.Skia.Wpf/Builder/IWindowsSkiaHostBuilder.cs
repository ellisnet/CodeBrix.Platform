#nullable enable

using System;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public interface IWindowsSkiaHostBuilder
{
	internal Func<System.Windows.Application>? WpfApplication { get; set; }
}
