#nullable enable

using System.Numerics;

namespace CodeBrix.Platform.UI.Composition //Was previously: Uno.UI.Composition
{
	internal interface ISizedBrush
	{
		internal Vector2? Size { get; }
	}
}
