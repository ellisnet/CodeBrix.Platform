#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeBrix.Platform.UI.Tasks.LinkerHintsGenerator
{
	/// <summary>
	/// Turns the application's trimming feature switches (the RuntimeHostConfigurationOption items marked
	/// <c>Trim="true"</c>) into the <c>--feature &lt;name&gt; &lt;value&gt;</c> arguments of the linker-hint passes.
	/// </summary>
	/// <remarks>
	/// The hint passes must see the same switches as the final ILLink run: a descriptor entry guarded by
	/// <c>feature="X" featurevalue="true" featuredefault="true"</c> (the Toolkit's CodeBrix.Platform.UI.Toolkit.RootXamlControls
	/// roots) otherwise stays rooted in pass 1, its types come out "available", and the final pass keeps the XAML that uses
	/// them even though the application switched the feature off.
	/// </remarks>
	internal static class LinkerFeatureSwitches
	{
		/// <summary>
		/// Formats the switches as linker arguments, in the order given. A switch with an empty name or value, or with
		/// white space in either (which the linker's response file would split), is skipped.
		/// </summary>
		/// <param name="switches">The switches: name and value.</param>
		/// <returns>The arguments, separated by spaces (empty when there is none).</returns>
		internal static string Format(IEnumerable<(string Name, string Value)> switches)
			=> string.Join(" ", switches
				.Where(s => IsUsable(s.Name) && IsUsable(s.Value))
				.Select(s => $"--feature {s.Name} {s.Value}"));

		private static bool IsUsable(string text)
			=> !string.IsNullOrEmpty(text) && !text.Any(char.IsWhiteSpace);
	}
}
