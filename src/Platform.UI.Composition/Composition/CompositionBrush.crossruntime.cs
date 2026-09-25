#if !__NETSTD_REFERENCE__
#nullable enable

using CodeBrix.Platform.UI.Composition.Contracts;

namespace Microsoft.UI.Composition
{
	public partial class CompositionBrush
	{
		private ICompositionBrushPlatform? _platform;

		/// <summary>
		/// Gets the platform state of this brush (what paints it), created the first time it is needed.
		/// </summary>
		internal ICompositionBrushPlatform Platform => _platform ??= CompositionPlatformServices.Composition.CreateBrushPlatform(this);

		internal bool CanPaint() => Platform.CanPaint();

		internal bool RequiresRepaintOnEveryFrame => Platform.RequiresRepaintOnEveryFrame;

		private protected override void DisposeInternal()
		{
			base.DisposeInternal();

			_platform?.Dispose();
		}
	}
}
#endif
