#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

//was previously: ICSharpCode.AvalonEdit/Utils/Empty.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Provides immutable empty list instances.
/// </summary>
static class Empty<T>
{
	/// <summary>The shared empty array instance.</summary>
	public static readonly T[] Array = new T[0];
	//public static readonly ReadOnlyCollection<T> ReadOnlyCollection = new ReadOnlyCollection<T>(Array);
}
