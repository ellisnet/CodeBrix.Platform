#if !__NETSTD_REFERENCE__
#nullable enable

using System.Numerics;
using CodeBrix.Platform.UI.Composition;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSurfaceBrush : CompositionBrush, ISizedBrush
	{
		// The tint of a monochrome image (a BitmapIcon shown as monochrome); null for a normal image.
		private Color? _monochromeColor;

		/// <summary>
		/// Gets or sets the tint that the surface is painted with when it is shown as a monochrome image, or
		/// <see langword="null"/> when it is painted with its own colors.
		/// </summary>
		internal Color? MonochromeTint
		{
			get => _monochromeColor;
			set => SetObjectProperty(ref _monochromeColor, value);
		}

		Vector2? ISizedBrush.Size => Platform.Size;
	}
}
#endif
