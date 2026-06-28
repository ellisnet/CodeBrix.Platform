namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal enum XamlNodeType
	{
		None,
		StartObject,
		GetObject,
		EndObject,
		StartMember,
		EndMember,
		Value,
		NamespaceDeclaration
	}
}
