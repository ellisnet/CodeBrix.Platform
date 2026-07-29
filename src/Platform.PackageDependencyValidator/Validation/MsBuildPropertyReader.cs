using System.Xml.Linq;

namespace CodeBrix.Platform.PackageDependencyValidator.Validation;

/// <summary>
/// Reads literal &lt;PropertyGroup&gt; values out of an MSBuild file without evaluating it.
/// </summary>
/// <remarks>
/// Only used for the buildTransitive lock declaration, whose values are deliberately plain
/// literals with no conditions and no property references — see platform.winui.common.props.
/// </remarks>
public static class MsBuildPropertyReader
{
	/// <summary>Returns property name -&gt; literal value for every unconditional property found.</summary>
	public static Dictionary<string, string> ReadProperties(string msbuildFilePath)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		var properties = XDocument
			.Load(msbuildFilePath)
			.Descendants()
			.Where(e => e.Parent is not null && e.Parent.Name.LocalName == "PropertyGroup");

		foreach (var property in properties)
		{
			if (property.Attribute("Condition") is not null)
			{
				continue;
			}

			result[property.Name.LocalName] = property.Value.Trim();
		}

		return result;
	}
}
