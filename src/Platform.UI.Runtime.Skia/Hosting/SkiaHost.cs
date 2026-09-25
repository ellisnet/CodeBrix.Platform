using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.UI.Runtime.Skia; //Was previously: Uno.UI.Runtime.Skia

public abstract class SkiaHost : CodeBrixPlatformHost
{
	/// <summary>
	/// Registers the Skia implementations of the framework's platform contracts before the host (and the
	/// application it creates) uses any of them.
	/// </summary>
	protected SkiaHost()
	{
		global::CodeBrix.Platform.UI.Skia.SkiaPlatformBootstrap.EnsureRegistered();
	}
}
