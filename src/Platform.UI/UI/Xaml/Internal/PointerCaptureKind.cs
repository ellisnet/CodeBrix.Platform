#nullable enable

using System;
using System.Linq;

namespace CodeBrix.Platform.UI.Xaml.Core; //Was previously: Uno.UI.Xaml.Core

[Flags]
internal enum PointerCaptureKind : byte
{
	None = 0,

	Explicit = 1,
	Implicit = 2,

	Any = Explicit | Implicit,
}
