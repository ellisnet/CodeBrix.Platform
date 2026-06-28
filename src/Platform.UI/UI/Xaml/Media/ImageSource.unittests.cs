using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CodeBrix.Platform;
using CodeBrix.Platform.Diagnostics.Eventing;
using CodeBrix.Platform.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

#if !IS_CODEBRIX
using CodeBrix.Platform.Web.Query;
using CodeBrix.Platform.Web.Query.Cache;
#endif

namespace Microsoft.UI.Xaml.Media;

partial class ImageSource
{
	protected ImageSource()
	{
	}

	partial void InitFromResource(Uri uri)
	{
		AbsoluteUri = uri;
	}

	partial void CleanupResource()
	{
		AbsoluteUri = null;
	}
}
