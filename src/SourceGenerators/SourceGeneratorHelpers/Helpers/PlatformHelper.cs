#nullable enable

using System;
using Microsoft.CodeAnalysis;
using CodeBrix.Platform.Roslyn;

namespace CodeBrix.Platform.UI.SourceGenerators.Helpers //Was previously: Uno.UI.SourceGenerators.Helpers
{
	public class PlatformHelper
	{
		public static bool IsValidPlatform(GeneratorExecutionContext context)
		{
			// Those two checks are now required since VS 16.9 which enables source generators by default
			// and the uno targets files are not present for uap targets.
			var isWindowsRuntimeApplicationOutput = context.Compilation.Options.OutputKind == OutputKind.WindowsRuntimeApplication;
			var isWindowsRuntimeMetadataOutput = context.Compilation.Options.OutputKind == OutputKind.WindowsRuntimeMetadata;

			return !isWindowsRuntimeMetadataOutput
				&& !isWindowsRuntimeApplicationOutput;
		}

		public static bool IsCodeBrixHead(GeneratorExecutionContext context)
			=> context.GetMSBuildPropertyValue("IsCodeBrixHead")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

		/// <remarks>
		/// An application is a project with <c>IsCodeBrixHead=true</c>. The Android-app flag
		/// (<c>AndroidApplication</c>) is deliberately not consulted: this family has no Android-native head,
		/// and an Android application built on the Core assemblies marks itself with <c>IsCodeBrixHead</c>.
		/// </remarks>
		public static bool IsApplication(GeneratorExecutionContext context)
			=> IsCodeBrixHead(context);
	}
}
