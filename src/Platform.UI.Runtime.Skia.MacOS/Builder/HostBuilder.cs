using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia;
using Windows.UI.WebUI;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public static class HostBuilder
{
	public static ICodeBrixPlatformHostBuilder UseMacOS(this ICodeBrixPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new MacOSHostBuilder());
		return builder;
	}
}
