#if !__NETSTD_REFERENCE__
using System;
using System.Diagnostics.CodeAnalysis;
using Windows.ApplicationModel.VoiceCommands;

namespace Microsoft.UI.Composition;

internal partial class CompositionBrushWrapper : CompositionBrush
{
	private CompositionBrush _wrappedBrush;

	internal CompositionBrush WrappedBrush
	{
		get => _wrappedBrush;
		set => SetProperty(ref _wrappedBrush, value);
	}

	internal CompositionBrushWrapper(CompositionBrush wrappedBrush, Compositor compositor) : base(compositor)
	{
		WrappedBrush = wrappedBrush;
	}
}
#endif
