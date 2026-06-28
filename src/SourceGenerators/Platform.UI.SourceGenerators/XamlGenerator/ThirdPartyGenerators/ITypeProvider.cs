#nullable enable

using Microsoft.CodeAnalysis;

namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators; //Was previously: Uno.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators

internal interface ITypeProvider
{
	ITypeSymbol? TryGetType(ITypeSymbol symbol, string memberName);
}
