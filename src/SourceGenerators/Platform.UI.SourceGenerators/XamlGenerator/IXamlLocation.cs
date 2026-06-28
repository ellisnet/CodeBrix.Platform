#nullable enable

namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator //Was previously: Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Used as simple way of identifying the source location of an element in XAML
	/// </summary>
	internal interface IXamlLocation
	{
		public string FilePath { get; }

		public int LineNumber { get; }

		public int LinePosition { get; }
	}

}
