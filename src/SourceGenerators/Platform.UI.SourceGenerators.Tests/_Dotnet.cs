using System;
using System.Linq;
using Microsoft.CodeAnalysis.Testing;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests; //Was previously: Uno.UI.SourceGenerators.Tests

internal record class _Dotnet(string Moniker, ReferenceAssemblies ReferenceAssemblies)
{
	public ReferenceAssemblies WithCodeBrixPackage(string version = "1.0.183.233")
		=> ReferenceAssemblies.AddPackages([new PackageIdentity("CodeBrix.Platform.ApacheLicenseForever", version)]);

	public static _Dotnet Current = new("net10.0", ReferenceAssemblies.Net.Net100);
}
