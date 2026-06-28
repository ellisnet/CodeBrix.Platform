#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
	internal sealed class RequiredMemberAttribute : Attribute;
	internal sealed class CompilerFeatureRequiredAttribute(string name) : Attribute
	{
		internal string Name { get; } = name;
	}
}

namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.Constructor)]
	internal sealed class SetsRequiredMembersAttribute : Attribute;
}
#endif
