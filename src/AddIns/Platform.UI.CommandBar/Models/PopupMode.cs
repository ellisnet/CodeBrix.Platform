namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// How a <see cref="ToolDropDownButton"/> divides its behaviour between running its command and
/// opening its flyout.
/// </summary>
/// <remarks>
/// The three modes are the ones desktop tool bars have converged on; a menu of recently opened
/// files behind a button that itself opens a file is <see cref="MenuButton"/>, a pure chooser is
/// <see cref="Instant"/>, and a button whose menu is a rarely wanted alternative to its default
/// action is <see cref="Delayed"/>.
/// </remarks>
public enum PopupMode
{
	/// <summary>The button has two parts: the main part runs the command, a separate arrow part
	/// opens the flyout. The default.</summary>
	MenuButton,

	/// <summary>The whole button opens the flyout; no command runs.</summary>
	Instant,

	/// <summary>A press-and-release runs the command; a press held past the delay opens the flyout
	/// instead.</summary>
	Delayed,
}
