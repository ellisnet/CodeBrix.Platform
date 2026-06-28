#nullable enable

namespace CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging //Was previously: Uno.UI.Adapter.Microsoft.Extensions.Logging
{
	using System;
	using System.Runtime.CompilerServices;

	public class LoggingAdapter
	{
		// [ModuleInitializer]
		public static void Initialize()
		{
			Foundation.Logging.LoggerFactory.ExternalLoggerFactory = new MicrosoftLoggerFactory();
		}
	}
}
