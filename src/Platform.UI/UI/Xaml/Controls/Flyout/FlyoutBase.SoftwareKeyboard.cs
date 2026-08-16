namespace Microsoft.UI.Xaml.Controls.Primitives
{
	partial class FlyoutBase
	{
		/// <summary>
		/// Declares that this flyout's opening and closing must not change whether
		/// the software (on-screen) keyboard is showing. A text-entry control whose
		/// focus moves into a flyout marked this way keeps its keyboard up for the
		/// flyout's lifetime — the way the built-in text-selection context menu
		/// behaves — instead of hiding it on open and re-showing it on close.
		/// The mark is honored by the control that OWNS the flyout (TextBox and
		/// PasswordBox honor it on their built-in context menu); heads without a
		/// software keyboard ignore it entirely. Default is false.
		/// </summary>
		public bool DoesNotAffectSoftwareKeyboard { get; set; }
	}
}
