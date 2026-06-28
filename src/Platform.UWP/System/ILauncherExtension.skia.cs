#nullable enable

using System;
using System.Threading.Tasks;
using Windows.System;

namespace CodeBrix.Platform.Extensions.System
{
	internal interface ILauncherExtension
	{
		Task<bool> LaunchUriAsync(Uri uri);
		Task<LaunchQuerySupportStatus> QueryUriSupportAsync(Uri uri, LaunchQuerySupportType launchQuerySupportType);
	}
}
