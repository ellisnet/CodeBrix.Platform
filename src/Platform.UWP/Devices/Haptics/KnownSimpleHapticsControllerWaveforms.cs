namespace Windows.Devices.Haptics
{
	/// <summary>
	/// Provides a set of well-known haptic waveform types
	/// (based on the Haptic Usage Page HID specification).
	/// </summary>
	/// <remarks>
	/// Each value is the waveform's usage ID on the HID Haptics usage page (0x0E), which is what WinUI returns and what
	/// <see cref="SimpleHapticsControllerFeedback.Waveform"/> carries for a device that supports the waveform.
	/// </remarks>
	public static partial class KnownSimpleHapticsControllerWaveforms
	{
		/// <summary>
		/// Gets a buzz waveform that is generated continuously
		/// without interruption until terminated.
		/// </summary>
		public static ushort BuzzContinuous => 0x1004;

		/// <summary>
		/// Gets a click waveform.
		/// </summary>
		public static ushort Click => 0x1003;

		/// <summary>
		/// Gets a press waveform.
		/// </summary>
		public static ushort Press => 0x1006;

		/// <summary>
		/// Gets a release waveform.
		/// </summary>
		public static ushort Release => 0x1007;

		/// <summary>
		/// Gets a rumble waveform that is generated continuously
		/// without interruption until terminated.
		/// </summary>
		public static ushort RumbleContinuous => 0x1005;

		/// <summary>
		/// Gets a waveform played when the pointer hovers over an interactive element.
		/// </summary>
		public static ushort Hover => 0x1008;

		/// <summary>
		/// Gets a waveform (an ascending pattern) that confirms a completed action.
		/// </summary>
		public static ushort Success => 0x1009;

		/// <summary>
		/// Gets a waveform (a descending pattern) that indicates a failed action.
		/// </summary>
		public static ushort Error => 0x100A;

		/// <summary>
		/// Gets a waveform generated continuously while inking with a ballpoint pen.
		/// </summary>
		public static ushort InkContinuous => 0x100B;

		/// <summary>
		/// Gets a waveform generated continuously while inking with a pencil.
		/// </summary>
		public static ushort PencilContinuous => 0x100C;

		/// <summary>
		/// Gets a waveform generated continuously while inking with a marker.
		/// </summary>
		public static ushort MarkerContinuous => 0x100D;

		/// <summary>
		/// Gets a waveform generated continuously while inking with a chisel marker or highlighter.
		/// </summary>
		public static ushort ChiselMarkerContinuous => 0x100E;

		/// <summary>
		/// Gets a waveform generated continuously while inking with a brush.
		/// </summary>
		public static ushort BrushContinuous => 0x100F;

		/// <summary>
		/// Gets a waveform generated continuously while erasing.
		/// </summary>
		public static ushort EraserContinuous => 0x1010;

		/// <summary>
		/// Gets a waveform generated continuously for special ink tools, such as a multi-colored brush
		/// (the HID "Sparkle Continuous" waveform).
		/// </summary>
		public static ushort GalaxyPenContinuous => 0x1011;
	}
}
