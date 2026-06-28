using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeBrix.Platform.UI.SourceGenerators; //Was previously: Uno.UI.SourceGenerators

[Generator]
internal sealed class UseOpenSansGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var useOpenSansProvider = context.AnalyzerConfigOptionsProvider.Select(static (provider, ct) =>
		{
			provider.GlobalOptions.TryGetValue("build_property.CodeBrixDefaultFont", out var value);
			return value?.Equals("OpenSans", StringComparison.OrdinalIgnoreCase) == true;
		});

		context.RegisterSourceOutput(useOpenSansProvider, (context, useOpenSans) =>
		{
			if (useOpenSans)
			{
				context.AddSource("CodeBrixUseOpenSansGenerator.g.cs", """
					internal static class __CodeBrixUseOpenSansInitializer
					{
						[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]
						internal static void Initialize()
						{
							global::CodeBrix.Platform.UI.FeatureConfiguration.Font.DefaultTextFontFamily = "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans.ttf";
						}
					}
					""");
			}
		});
	}
}
