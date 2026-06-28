using System;
using CodeBrix.Platform;
using CodeBrix.Platform.Extensions;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Activation;

[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
public sealed partial class SearchActivatedEventArgs : IActivatedEventArgs
{
	public ActivationKind Kind => ActivationKind.Search;

	[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public ApplicationExecutionState PreviousExecutionState { get; }

	[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public SplashScreen SplashScreen { get; }

	[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public int CurrentlyShownApplicationViewId { get; }

	[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public string Language { get; }

	[NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public string QueryText { get; }
}
