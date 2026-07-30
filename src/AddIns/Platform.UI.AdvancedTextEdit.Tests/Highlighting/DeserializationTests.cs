#nullable enable

using System.IO;
using System.Xml;

using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;
using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Highlighting;

//was previously: ICSharpCode.AvalonEdit.Tests/Highlighting/DeserializationTests.cs in the AvalonEdit repo (MIT).
//DROPPED: TestRoundTripColor - it round-tripped HighlightingColor through Json.NET, which serializes
//that type via its ISerializable implementation; the port deliberately dropped
//[Serializable]/ISerializable, so serialization round-trip tests are dropped with it.
//ADAPTED: XshdSerializationDoesNotCrash keeps its Xshd resource-loading coverage (name and
//extensions of the loaded C# definition) but drops the final JsonConvert.SerializeObject smoke
//assertion for the same reason. The upstream [SetUp] document/highlighter served only the dropped
//test and is not carried over.

/// <summary>
/// Exercises loading a built-in Xshd syntax definition from the embedded resources.
/// </summary>
public class DeserializationTests
{
	[Theory]
	[InlineData("CSharp-Mode.xshd")]
	public void loading_xshd_resource_yields_the_csharp_definition(string resourceName) // XshdSerializationDoesNotCrash
	{
		//Arrange + Act
		XshdSyntaxDefinition xshd;
		using (Stream s = Resources.OpenStream(resourceName))
		{
			using (XmlTextReader reader = new XmlTextReader(s))
			{
				xshd = HighlightingLoader.LoadXshd(reader, false);
			}
		}

		//Assert
		Assert.Equal("C#", xshd.Name);
		Assert.NotEmpty(xshd.Extensions);
		Assert.Equal(".cs", xshd.Extensions[0]);
	}
}
