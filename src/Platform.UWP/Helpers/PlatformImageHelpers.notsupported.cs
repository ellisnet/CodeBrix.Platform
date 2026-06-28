#if !__CROSSRUNTIME__
using System;
using System.Threading.Tasks;
using Windows.Graphics.Display;

namespace CodeBrix.Platform.Helpers; //Was previously: Uno.Helpers

internal static partial class PlatformImageHelpers
{
	internal static Task<string> GetScaledPath(Uri uri, ResolutionScale? scaleOverride)
		=> Task.FromResult(uri.OriginalString);
}
#endif
