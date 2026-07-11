using Microsoft.CodeAnalysis;

//CodeBrix warning-cleanup 2026-07-10: descriptors kept as-is (English-only strings, existing help-link conventions, no customTags); RS1007/RS1015/RS1028 suggestions suppressed rather than restructured.
#pragma warning disable RS1007, RS1015, RS1028
namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator //Was previously: Uno.UI.SourceGenerators.XamlGenerator
{
	public static class XamlCodeGenerationDiagnostics
	{
		internal const string Title = "XAML Generation Failed";
		internal const string MessageFormat = "{0}";
		internal const string XamlGenerationFailureDescription = "XAML Generation Failed.";
		internal const string XamlCategory = "XAML";
		internal const string ResourcesCategory = "Resources";

		public static readonly DiagnosticDescriptor GenericXamlErrorRule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
																						   "UXAML0001",
#pragma warning restore RS2008 // Enable analyzer release tracking
																						   Title,
																						   MessageFormat,
																						   XamlCategory,
																						   DiagnosticSeverity.Error,
																						   isEnabledByDefault: true,
																						   description: XamlGenerationFailureDescription
																						  );

		public static readonly DiagnosticDescriptor GenericXamlWarningRule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
																							 "UXAML0002",
#pragma warning restore RS2008 // Enable analyzer release tracking
																							 Title,
																							 MessageFormat,
																							 XamlCategory,
																							 DiagnosticSeverity.Warning,
																							 isEnabledByDefault: true,
																							 description: XamlGenerationFailureDescription
																							);

		public static readonly DiagnosticDescriptor ResourceParsingFailureRule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
																							 "UXAML0003",
#pragma warning restore RS2008 // Enable analyzer release tracking
																							 Title,
																							 MessageFormat,
																							 ResourcesCategory,
																							 DiagnosticSeverity.Error,
																							 isEnabledByDefault: true,
																							 description: "Resource Generation Failed."
																							);
	}
}
