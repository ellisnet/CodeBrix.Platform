using Windows.UI;
using Microsoft.UI.Xaml.Input;

namespace CodeBrix.Platform.UI.Xaml.Core; //Was previously: Uno.UI.Xaml.Core

internal interface IRootElement
{
	void SetBackgroundColor(Color backgroundColor);

	/// <summary>
	/// Height, in logical pixels, withheld from the BOTTOM of every root except the
	/// popup root during measure and arrange. Zero (the default) is byte-for-byte
	/// the historical behavior. A host that displays its own chrome over the bottom
	/// of the window (an on-screen software keyboard, say) sets this so application
	/// content re-lays-out into the remaining space — content can then never sit
	/// under that chrome — while popups keep the full window to draw into.
	/// </summary>
	double ContentBottomOcclusionInset { get; set; }
}
