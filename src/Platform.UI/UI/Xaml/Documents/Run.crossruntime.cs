#if !__NETSTD_REFERENCE__
#nullable enable

namespace Microsoft.UI.Xaml.Documents
{
	partial class Run
	{
		/// <summary>
		/// The text engine's shaped segments of this run, opaque to platform-neutral code: the engine creates them on
		/// first use and keeps them here; they are cleared whenever the text or a font property of the run changes.
		/// </summary>
		internal object? PlatformSegments { get; set; }

		public global::Microsoft.UI.Xaml.FlowDirection FlowDirection
		{
			get => (global::Microsoft.UI.Xaml.FlowDirection)this.GetValue(FlowDirectionProperty);
			set => this.SetValue(FlowDirectionProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty FlowDirectionProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				nameof(FlowDirection), typeof(FlowDirection),
				typeof(Run),
				new FrameworkPropertyMetadata(default(FlowDirection), FrameworkPropertyMetadataOptions.Inherits, (DependencyObject dO, DependencyPropertyChangedEventArgs args) => ((Run)dO).OnFlowDirectionChanged()));

		private void OnFlowDirectionChanged()
		{
			InvalidateInlines(false);
		}

		partial void InvalidateSegmentsPartial() => PlatformSegments = null;
	}
}
#endif
