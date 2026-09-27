#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// The host-free Core proof over EVERY Core assembly the family produces (plan gate G6, Phase D): the framework's
/// Core assemblies and every add-in's Core assembly are read from their build output (metadata only - nothing is
/// loaded, so the SkiaSharp an R7 Core references stays out of this process) and their assembly references are
/// checked. No Core assembly may reference a Skia assembly of the platform (the framework's Skia twins, the heads,
/// or any add-in's platform assembly). A Core assembly that is not a Skia-canvas add-in Core (plan rule R7) may not
/// reference SkiaSharp or HarfBuzzSharp either, directly or through another Core assembly. The list of assemblies
/// comes from this project's csproj (the CoreAssembly assembly metadata).
/// </summary>
public class CoreAssemblyReferenceTests
{
	/// <summary>Every Core assembly of the family: the framework's twelve and the add-ins' fourteen (8 of them R7).</summary>
	private static readonly string[] ExpectedCoreAssemblies =
	{
		"CodeBrix.Platform.UI.Core", "CodeBrix.Platform.UI.Composition.Core", "CodeBrix.Platform.Core",
		"CodeBrix.Platform.UI.Dispatching.Core", "CodeBrix.Platform.UI.Toolkit.Core", "CodeBrix.Platform.Foundation.Core",
		"CodeBrix.Platform.Foundation.Logging.Core", "CodeBrix.Platform.UI.FluentTheme.Core",
		"CodeBrix.Platform.UI.FluentTheme.v1.Core", "CodeBrix.Platform.UI.FluentTheme.v2.Core",
		"CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging.Core", "CodeBrix.Platform.Extensions.Logging.Core",
		"CodeBrix.Platform.UI.FlexPanel.Core", "CodeBrix.Platform.UI.CommandBar.Core", "CodeBrix.Platform.AppSettings.Core",
		"CodeBrix.Platform.UI.AudioPlayer.Core", "CodeBrix.Platform.WinUI.Graphics3DGL.Core", "CodeBrix.Platform.UI.VideoPlayer.Core",
		"CodeBrix.Platform.SkiaSharp.Views.Core", "CodeBrix.Platform.WinUI.Graphics2DSK.Core", "CodeBrix.Platform.UI.Svg.Core",
		"CodeBrix.Platform.UI.Lottie.Core", "CodeBrix.Platform.UI.TextLayout.Core", "CodeBrix.Platform.UI.AdvancedTextEdit.Core",
		"CodeBrix.Platform.UI.TerminalView.Core", "CodeBrix.Platform.UI.PlotterView.Core",
	};

	/// <summary>The platform's Skia-side assemblies: the framework's Skia twins and every add-in's platform assembly.</summary>
	private static readonly string[] PlatformSkiaAssemblies =
	{
		"CodeBrix.Platform.UI", "CodeBrix.Platform.UI.Composition", "CodeBrix.Platform", "CodeBrix.Platform.UI.Dispatching",
		"CodeBrix.Platform.UI.Toolkit", "CodeBrix.Platform.UI.XamlHost", "CodeBrix.Platform.UI.XamlHost.Skia.Wpf",
		"CodeBrix.Platform.UI.CommandBar", "CodeBrix.Platform.AppSettings", "CodeBrix.Platform.UI.AudioPlayer.Skia",
		"CodeBrix.Platform.SkiaSharp.Views", "CodeBrix.Platform.UI.Svg", "CodeBrix.Platform.UI.Lottie",
		"CodeBrix.Platform.UI.TextLayout", "CodeBrix.Platform.UI.AdvancedTextEdit", "CodeBrix.Platform.UI.TerminalView",
		"CodeBrix.Platform.UI.PlotterView", "CodeBrix.Platform.WinUI.Graphics3DGL", "CodeBrix.Platform.UI.VideoPlayer.Skia",
		"CodeBrix.Platform.UI.WebView.Skia", "CodeBrix.Platform.UI.MediaPlayer.Skia",
		// Pre-split names of the Core-by-whole libraries: they must not come back as references either.
		"CodeBrix.Platform.Foundation", "CodeBrix.Platform.Foundation.Logging", "CodeBrix.Platform.UI.FluentTheme",
		"CodeBrix.Platform.UI.FluentTheme.v1", "CodeBrix.Platform.UI.FluentTheme.v2",
		"CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging", "CodeBrix.Platform.Extensions.Logging",
		"CodeBrix.Platform.UI.FlexPanel", "CodeBrix.Platform.WinUI.Graphics2DSK",
	};

	/// <summary>Non-Core family assemblies a Core may reference: build-time tooling kept whole (P14) and the managed GL binding.</summary>
	private static readonly string[] AllowedNonCoreFamilyAssemblies = { "CodeBrix.Platform.Xaml", "CodeBrix.Platform.OpenGL" };

	public static IEnumerable<object[]> CoreAssemblies() =>
		CoreAssemblyEntries().Keys.OrderBy(k => k, StringComparer.Ordinal).Select(k => new object[] { k });

	[Fact]
	public void When_The_Core_Assembly_List_Is_Read_Then_It_Names_Every_Core_Assembly_Of_The_Family_And_Each_Is_Built()
	{
		//Arrange
		var entries = CoreAssemblyEntries();

		//Act
		var missingFiles = entries.Where(e => !File.Exists(e.Value.Path)).Select(e => e.Value.Path).ToArray();

		//Assert
		entries.Keys.Should().BeEquivalentTo(ExpectedCoreAssemblies);
		entries.Values.Count(v => v.IsR7).Should().Be(8);
		missingFiles.Should().BeEmpty();
	}

	[Theory]
	[MemberData(nameof(CoreAssemblies))]
	public void When_A_Core_Assembly_Is_Read_Then_It_References_No_Skia_Assembly_Of_The_Platform(string coreAssembly)
	{
		//Arrange
		var entries = CoreAssemblyEntries();
		var entry = entries[coreAssembly];

		//Act
		var (name, references) = ReadReferences(entry.Path);
		var closure = CoreClosure(coreAssembly, entries);

		//Assert
		name.Should().Be(coreAssembly);
		references.Should().NotContain(PlatformSkiaAssemblies);
		references.Should().NotContain(n => n.StartsWith("CodeBrix.Platform.UI.Runtime.Skia", StringComparison.Ordinal));
		references.Where(n => n.StartsWith("CodeBrix.Platform", StringComparison.Ordinal))
			.Where(n => !entries.ContainsKey(n) && !AllowedNonCoreFamilyAssemblies.Contains(n))
			.Should().BeEmpty();
		if (!entry.IsR7)
		{
			references.Should().NotContain(n => n.StartsWith("SkiaSharp", StringComparison.Ordinal) || n.StartsWith("HarfBuzzSharp", StringComparison.Ordinal));
			closure.Should().NotContain(n => entries[n].IsR7);
		}
	}

	/// <summary>Every Core assembly reachable from <paramref name="root"/> through Core-to-Core references (root excluded).</summary>
	private static HashSet<string> CoreClosure(string root, IReadOnlyDictionary<string, (bool IsR7, string Path)> entries)
	{
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var pending = new Stack<string>();
		pending.Push(root);
		while (pending.Count > 0)
		{
			var current = pending.Pop();
			foreach (var reference in ReadReferences(entries[current].Path).References.Where(entries.ContainsKey))
			{
				if (reference != root && seen.Add(reference))
				{
					pending.Push(reference);
				}
			}
		}
		return seen;
	}

	private static (string Name, string[] References) ReadReferences(string path)
	{
		using var stream = File.OpenRead(path);
		using var pe = new PEReader(stream);
		var metadata = pe.GetMetadataReader();
		var name = metadata.GetString(metadata.GetAssemblyDefinition().Name);
		var references = metadata.AssemblyReferences
			.Select(h => metadata.GetString(metadata.GetAssemblyReference(h).Name))
			.ToArray();
		return (name, references);
	}

	internal static Dictionary<string, (bool IsR7, string Path)> CoreAssemblyEntries() =>
		typeof(CoreAssemblyReferenceTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
			.Where(a => a.Key.StartsWith("CoreAssembly:", StringComparison.Ordinal))
			.ToDictionary(
				a => a.Key.Substring("CoreAssembly:".Length),
				a =>
				{
					var parts = a.Value!.Split('|', 2);
					return (parts[0] == "R7", Path.GetFullPath(parts[1]));
				},
				StringComparer.Ordinal);
}
