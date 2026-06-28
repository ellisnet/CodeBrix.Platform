namespace CodeBrix.Platform;

partial class WinRTFeatureConfiguration
{
	public static class MessageDialog
	{
		/// <summary>
		/// Set this flag to true to use native OS dialogs when displaying MessageDialog.
		/// Note the native dialogs may not support all the features and they are also not
		/// supported on Skia targets.
		/// </summary>
		public static bool UseNativeDialog { get; set; }
#if false
			= true;
#endif

		/// <summary>
		/// Allows overriding the style used by the ContentDialog
		/// which displays the MessageDialog. Should be set to a name (Key)
		/// of a Application-level ContentDialog style resource.
		/// </summary>
		public static string StyleOverride { get; set; }
	}
}
