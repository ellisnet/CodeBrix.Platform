extern alias __codebrix;

namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator.XamlRedirection //Was previously: Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class NamespaceDeclaration
	{
		private __codebrix::CodeBrix.Platform.Xaml.NamespaceDeclaration _codebrixNs;

		public NamespaceDeclaration(__codebrix::CodeBrix.Platform.Xaml.NamespaceDeclaration ns)
			=> _codebrixNs = ns;

		public string Namespace => _codebrixNs.Namespace;
		public string Prefix => _codebrixNs.Prefix;
	}
}
