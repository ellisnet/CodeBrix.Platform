using System;
using Windows.UI.Text;

namespace CodeBrix.Platform.UI.Xaml.Media; //Was previously: Uno.UI.Xaml.Media

internal sealed class FontInfo
{
	public FontStyle FontStyle { get; set; }
	public ushort FontWeight { get; set; }
	public FontStretch FontStretch { get; set; }
	public string FamilyName { get; set; }
}
