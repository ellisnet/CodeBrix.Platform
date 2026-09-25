using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBrix.Platform.UWPSyncGenerator //Was previously: Uno.UWPSyncGenerator
{
	[Flags]
	public enum ImplementedFor
	{
		None = 0,
		Android = 1,
		iOS = 2,
		MacOS = 4,
		UnitTests = 8,
		NetStdReference = 16,
		WASM = 32,
		Skia = 64,
		UAP = 128,
		tvOS = 256,
		Uno = Android | iOS | MacOS | UnitTests | NetStdReference | WASM | Skia | tvOS,
		// CodeBrix.Platform's only main flavor is Skia (there are no Android/iOS/macOS/tvOS/WASM flavors).
		Main = Skia,
		Mobile = Android | iOS | tvOS,
		Xamarin = Android | iOS | MacOS | tvOS
	}
}
