extern alias __codebrix;

namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator.XamlRedirection //Was previously: Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlXmlReaderSettings
	{
		public XamlXmlReaderSettings()
		{
			CodeBrixInner = new __codebrix::CodeBrix.Platform.Xaml.XamlXmlReaderSettings();
		}

		public bool ProvideLineInfo
		{
			get => CodeBrixInner.ProvideLineInfo;
			set
			{
				CodeBrixInner.ProvideLineInfo = value;
			}
		}

		public __codebrix::CodeBrix.Platform.Xaml.XamlXmlReaderSettings CodeBrixInner { get; internal set; }
	}
}
