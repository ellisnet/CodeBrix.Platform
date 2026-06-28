using System.Reflection;
using CodeBrix.Platform.Extensions;
using ProcessorArchitecture = Windows.System.ProcessorArchitecture;

namespace Windows.ApplicationModel;

partial class PackageId
{
	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public ProcessorArchitecture Architecture => ProcessorArchitecture.Unknown;

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public string PublisherId => "Unknown";

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public string ResourceId => "Unknown";

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public string Author => "Unknown";

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public string ProductId => "Unknown";

#if true
	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS")]
	public string FamilyName => "Unknown";

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS")]
	public string FullName => "Unknown";
#endif

#if IS_UNIT_TESTS
	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS")]
	public string Name { get; internal set; } = "Unknown";

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS")]
	public PackageVersion Version { get; internal set; } = new PackageVersion(Assembly.GetExecutingAssembly().GetVersionNumber());
#endif

#if !__SKIA__ && !__CROSSRUNTIME__
	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS")]
	public string Publisher { get; internal set; } = "Unknown";
#endif
}
