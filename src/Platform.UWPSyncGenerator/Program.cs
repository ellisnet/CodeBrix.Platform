using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeBrix.Platform.UWPSyncGenerator //Was previously: Uno.UWPSyncGenerator
{
	class Program
	{
		const string SyncMode = "sync";
		const string DocMode = "doc";
		const string AllMode = "all";

		static async Task Main(string[] args)
		{
			Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

			DeleteDirectoryIfExists(@"..\..\..\Platform.UI\Generated\");
			DeleteDirectoryIfExists(@"..\..\..\Platform.UWP\Generated\");
			DeleteDirectoryIfExists(@"..\..\..\Platform.Foundation\Generated\");
			DeleteDirectoryIfExists(@"..\..\..\Platform.UI.Composition\Generated\");
			DeleteDirectoryIfExists(@"..\..\..\Platform.UI.Dispatching\Generated\");

			if (args.Length == 0)
			{
				Console.WriteLine("No mode selected. Supported modes: doc, sync & all.");
				return;
			}

			var mode = args[0].ToLowerInvariant();

			if (mode == SyncMode || mode == AllMode)
			{
				await new SyncGenerator().Build("CodeBrix.Platform.Foundation", "Windows.Foundation.FoundationContract");
				await new SyncGenerator().Build("CodeBrix", "Windows.Foundation.UniversalApiContract");
				await new SyncGenerator().Build("CodeBrix", "Windows.Phone.PhoneContract");
				await new SyncGenerator().Build("CodeBrix", "Windows.Networking.Connectivity.WwanContract");
				await new SyncGenerator().Build("CodeBrix", "Windows.ApplicationModel.Calls.CallsPhoneContract");
				await new SyncGenerator().Build("CodeBrix", "Windows.Services.Store.StoreContract");
				await new SyncGenerator().Build("CodeBrix", "Microsoft.Windows.AppLifecycle");

				// When adding support for a new WinRT contract here, ensure to add it to the list of supported contracts in ApiInformation.shared.cs

#if HAS_CODEBRIX_WINUI
				await new SyncGenerator().Build("CodeBrix.Platform.Foundation", "Microsoft.Foundation");

				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.Foundation");

				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.Graphics");
				await new SyncGenerator().Build("CodeBrix.Platform.UI.Dispatching", "Microsoft.UI.Dispatching");
				await new SyncGenerator().Build("CodeBrix.Platform.UI.Composition", "Microsoft.UI.Composition");
				await new SyncGenerator().Build("CodeBrix.Platform.UI.Dispatching", "Microsoft.UI");
				await new SyncGenerator().Build("CodeBrix.Platform.UI.Composition", "Microsoft.UI");

				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Text");
				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Content");
				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.Windows.ApplicationModel.Resources");
				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.Web.WebView2.Core");

				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Input");
				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI");
				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Windowing");

				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Xaml");

#else
				await new SyncGenerator().Build("CodeBrix.Platform.UI.Composition", "Windows.Foundation.UniversalApiContract");
				await new SyncGenerator().Build("CodeBrix.Platform.UI.Dispatching", "Windows.Foundation.UniversalApiContract");
				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Windows.Foundation.UniversalApiContract");
				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Windows.UI.Xaml.Hosting.HostingContract");
				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Xaml");
				await new SyncGenerator().Build("CodeBrix.Platform.UI", "Microsoft.Web.WebView2.Core");
#endif
			}

			if (mode == DocMode || mode == AllMode)
			{
#if HAS_CODEBRIX_WINUI
				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Content");
				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.Windows.ApplicationModel.Resources");
				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.Web.WebView2.Core");

				await new DocGenerator().Build("CodeBrix.Platform.UI.Dispatching", "Microsoft.UI.Dispatching");
				await new DocGenerator().Build("CodeBrix.Platform.UI.Composition", "Microsoft.UI.Composition");

				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.Foundation");
				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Composition");
				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Dispatching");
				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Input");
				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.Graphics");
				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Windowing");
				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI");

				await new DocGenerator().Build("CodeBrix.Platform.UI", "Microsoft.UI.Xaml");
#else
				await new DocGenerator().Build("CodeBrix.Platform.UI", "Windows.Foundation.UniversalApiContract");
#endif
			}
		}

		private static void DeleteDirectoryIfExists(string path)
		{
			if (Directory.Exists(path))
				Directory.Delete(path, recursive: true);
		}
	}
}
