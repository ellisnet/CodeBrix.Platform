extern alias __codebrix;

namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator.XamlRedirection //Was previously: Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlSchemaContext
	{
		public XamlSchemaContext()
		{
			CodeBrixInner = new __codebrix::CodeBrix.Platform.Xaml.XamlSchemaContext();
		}

		public XamlSchemaContext(System.Collections.Generic.IEnumerable<System.Reflection.Assembly> enumerable)
		{
			CodeBrixInner = new __codebrix::CodeBrix.Platform.Xaml.XamlSchemaContext(enumerable);
		}

		public __codebrix::CodeBrix.Platform.Xaml.XamlSchemaContext CodeBrixInner { get; }
	}
}
