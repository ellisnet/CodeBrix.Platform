extern alias __codebrix;

using System;
using System.Xml;

namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator.XamlRedirection //Was previously: Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlXmlReader : IDisposable
	{
		private __codebrix::CodeBrix.Platform.Xaml.XamlXmlReader _codebrixReader;

		public XamlXmlReader(XmlReader document, XamlSchemaContext context, XamlXmlReaderSettings settings, __codebrix::CodeBrix.Platform.Xaml.IsIncluded isIncluded)
		{
			_codebrixReader = new __codebrix::CodeBrix.Platform.Xaml.XamlXmlReader(document, context.CodeBrixInner, settings.CodeBrixInner, isIncluded);
		}

		public bool DisableCaching => _codebrixReader.DisableCaching;

		public XamlNodeType NodeType => Convert(_codebrixReader.NodeType);

		private XamlNodeType Convert(__codebrix::CodeBrix.Platform.Xaml.XamlNodeType source)
			=> source switch
			{
				__codebrix::CodeBrix.Platform.Xaml.XamlNodeType.StartObject => XamlNodeType.StartObject,
				__codebrix::CodeBrix.Platform.Xaml.XamlNodeType.GetObject => XamlNodeType.GetObject,
				__codebrix::CodeBrix.Platform.Xaml.XamlNodeType.EndObject => XamlNodeType.EndObject,
				__codebrix::CodeBrix.Platform.Xaml.XamlNodeType.StartMember => XamlNodeType.StartMember,
				__codebrix::CodeBrix.Platform.Xaml.XamlNodeType.EndMember => XamlNodeType.EndMember,
				__codebrix::CodeBrix.Platform.Xaml.XamlNodeType.Value => XamlNodeType.Value,
				__codebrix::CodeBrix.Platform.Xaml.XamlNodeType.NamespaceDeclaration => XamlNodeType.NamespaceDeclaration,
				_ => XamlNodeType.None,
			};

		public object Value => _codebrixReader.Value;

		public XamlType Type => XamlType.FromType(_codebrixReader.Type);

		public int LineNumber => _codebrixReader.LineNumber;

		public int LinePosition => _codebrixReader.LinePosition;

		public XamlMember Member => XamlMember.FromMember(_codebrixReader.Member);

		public NamespaceDeclaration Namespace
			=> new NamespaceDeclaration(_codebrixReader.Namespace);

		public void Dispose() => ((IDisposable)_codebrixReader).Dispose();

		internal bool Read() => _codebrixReader.Read();

		internal bool PreserveWhitespace => _codebrixReader.PreserveWhitespace;
	}
}
