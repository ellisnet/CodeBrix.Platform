#nullable enable

using System;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using CodeBrix.Platform.Foundation.Logging;
using System.Threading;
using System.Globalization;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Xaml.Core;

namespace Microsoft.UI.Xaml
{
	public partial class Application
	{
		partial void InitializePartial()
		{
			InitializeSystemTheme();
		}
	}
}
