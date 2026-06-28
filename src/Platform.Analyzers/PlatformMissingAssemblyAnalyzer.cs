#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CodeBrix.Platform.Analyzers; //Was previously: Uno.Analyzers

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CodeBrixMissingAssemblyAnalyzer : DiagnosticAnalyzer
{
	internal const string Title = "An assembly required for a component is missing";
	internal const string MessageFormat = "Using '{0}' requires '{1}' NuGet package to be referenced";
	internal const string Category = "Correctness";

	internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
		"Uno0007",
#pragma warning restore RS2008 // Enable analyzer release tracking
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://aka.platform.uno/UNO0007"
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterCompilationStartAction(context =>
		{
			var assemblies = context.Compilation.ReferencedAssemblyNames.Select(a => a.Name).ToImmutableHashSet();
			var progressRing = context.Compilation.GetTypeByMetadataName("Microsoft.UI.Xaml.Controls.ProgressRing");
			_ = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.IsCodeBrixHead", out var isCodeBrixHead);
			if (isCodeBrixHead is null || !isCodeBrixHead.Equals("true", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			context.RegisterOperationAction(context =>
			{
				var objectCreation = (IObjectCreationOperation)context.Operation;
				if (objectCreation.Type is not INamedTypeSymbol type)
				{
					return;
				}

				if (type.DerivesFrom(progressRing) && !assemblies.Contains("CodeBrix.Platform.UI.Lottie"))
				{
					const string lottieNuGetPackageName = "CodeBrix.Platform.Lottie.ApacheLicenseForever";

					context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.Syntax.GetLocation(), "ProgressRing", lottieNuGetPackageName));
				}
			}, OperationKind.ObjectCreation);
		});
	}
}
