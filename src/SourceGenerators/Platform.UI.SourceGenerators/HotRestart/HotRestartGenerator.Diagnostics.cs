#nullable enable

using System;
using System.Globalization;
using Microsoft.CodeAnalysis;

//CodeBrix warning-cleanup 2026-07-10: descriptors kept as-is (English-only strings, existing help-link conventions); RS1007 (localizability) and RS1015 (helpLinkUri) suggestions suppressed rather than restructured.
#pragma warning disable RS1007, RS1015
namespace CodeBrix.Platform.UI.SourceGenerators.HotRestart; //Was previously: Uno.UI.SourceGenerators.HotRestart

public partial class HotRestartGenerator
{
	private static readonly DiagnosticDescriptor _descriptorMissingXClass = new(
#pragma warning disable RS2008 // Enable analyzer release tracking
		id: "Uno0004",
#pragma warning restore RS2008 // Enable analyzer release tracking
		title: "Unable to find x:Class on ApplicationDefinition",
		messageFormat: "Unable to find an x:Class attribute on the first node of the ApplicationDefinition",
		category: "Usage",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: WellKnownDiagnosticTags.NotConfigurable);

	private static readonly DiagnosticDescriptor _descriptorMissingAppDefinition = new(
#pragma warning disable RS2008 // Enable analyzer release tracking
		id: "Uno0005",
#pragma warning restore RS2008 // Enable analyzer release tracking
		title: "Unable to find an ApplicationDefinition file",
		messageFormat: "Unable to find an ApplicationDefinition file",
		category: "Usage",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: WellKnownDiagnosticTags.NotConfigurable);

}
