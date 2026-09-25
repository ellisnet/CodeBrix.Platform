#if !__NETSTD_REFERENCE__
#nullable enable

namespace Microsoft.UI.Xaml.Documents
{
	partial class Inline
	{
		/// <summary>
		/// The text engine's font for this inline, opaque to platform-neutral code: the engine creates it on first use
		/// and keeps it here; it is cleared whenever a font property of the inline changes.
		/// </summary>
		internal object? PlatformFontDetails { get; set; }

		protected override void OnFontFamilyChanged()
		{
			base.OnFontFamilyChanged();
			InvalidateInlines(false);
			InvalidateFontInfo();
		}

		protected override void OnFontStyleChanged()
		{
			base.OnFontStyleChanged();
			InvalidateInlines(false);
			InvalidateFontInfo();
		}

		protected override void OnFontStretchChanged()
		{
			base.OnFontStretchChanged();
			InvalidateInlines(false);
			InvalidateFontInfo();
		}

		protected override void OnFontWeightChanged()
		{
			base.OnFontWeightChanged();
			InvalidateInlines(false);
			InvalidateFontInfo();
		}

		protected override void OnFontSizeChanged()
		{
			base.OnFontSizeChanged();
			InvalidateInlines(false);
			InvalidateFontInfo();
		}

		private void InvalidateFontInfo() => PlatformFontDetails = null;
	}
}
#endif
