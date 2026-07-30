#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

//was previously: ICSharpCode.AvalonEdit/Utils/Boxes.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Reuse the same instances for boxed booleans.
/// </summary>
static class Boxes
{
	/// <summary>The shared boxed <c>true</c> instance.</summary>
	public static readonly object True = true;

	/// <summary>The shared boxed <c>false</c> instance.</summary>
	public static readonly object False = false;

	/// <summary>
	/// Gets the shared boxed instance for the specified boolean value.
	/// </summary>
	public static object Box(bool value)
	{
		return value ? True : False;
	}
}
