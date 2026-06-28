using System.ComponentModel;

namespace CodeBrix.Platform; //Was previously: Uno

/// <summary>
/// Marks a class or method as being preserved by the linker.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class PreserveAttribute : Attribute
{
	public bool AllMembers;
	public bool Conditional;

	public PreserveAttribute(bool allMembers, bool conditional)
	{
		AllMembers = allMembers;
		Conditional = conditional;
	}

	public PreserveAttribute()
	{
	}
}
