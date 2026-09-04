using CodeBrix.Platform.Simple;
using CodeBrix.Platform.UI.CommandBar;
using CommandBarDemo.Views;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.System;

// ReSharper disable CheckNamespace

namespace CommandBarDemo.ViewModels;

/// <summary>
/// The view model behind the demo's tool bars: one command per button, and the state the bars show.
/// </summary>
/// <remarks>
/// <para>
/// Two kinds of command are used on purpose, because a tool bar has to take both. New, Open and
/// Save are <c>XamlUICommand</c>s - the platform's "action object", carrying the label, the icon,
/// the description and the keyboard accelerator with them, so one command drives the tool bar
/// button, the menu item and the shortcut together. Everything else is a plain
/// <c>ICommand</c> (<c>SimpleCommand</c>), where the button states its own label and icon and the
/// command says only whether it can run.
/// </para>
/// <para>
/// Nothing here knows anything about tool bars beyond the icon sources it hands to its
/// <c>XamlUICommand</c>s; the view decides what a bar looks like.
/// </para>
/// </remarks>
[Microsoft.UI.Xaml.Data.Bindable]
public class MainViewModel : SimpleViewModel
{
	/// <summary>Initializes the demo's commands and starting state.</summary>
	public MainViewModel()
	{
		if (IsDesignMode(true)) { return; } //Leave as the first line of constructor

		NewCommand = UiCommand("New", "Start a new score", "new", VirtualKey.N, VirtualKeyModifiers.Control);
		OpenCommand = UiCommand("Open", "Open a score from disk", "open", VirtualKey.O, VirtualKeyModifiers.Control);
		SaveCommand = UiCommand("Save", "Save the current score", "save", VirtualKey.S, VirtualKeyModifiers.Control);

		NewCommand.ExecuteRequested += (_, _) => Record("New");
		OpenCommand.ExecuteRequested += (_, _) => Record("Open");
		SaveCommand.ExecuteRequested += (_, _) => Record("Save");

		SaveAsCommand = new SimpleCommand(() => Record("Save as"));
		PrintCommand = new SimpleCommand(() => CanPrint, () => Record("Print"));
		EngraveCommand = new SimpleCommand(() => Record("Engrave"));
		OpenRecentCommand = new SimpleCommand(file => Record("Open recent: " + file));
		EngraveModeCommand = new SimpleCommand(mode => Record("Engrave mode: " + mode));

		PreviousPageCommand = new SimpleCommand(() => PageNumber > 1, () => PageNumber--);
		NextPageCommand = new SimpleCommand(() => PageNumber < PageCount, () => PageNumber++);
	}

	/// <summary>The scores the inline chooser offers.</summary>
	public IReadOnlyList<string> Scores { get; } = new[]
	{
		"Prelude in C", "Goldberg Variations", "Clair de Lune", "Gymnopedie No. 1",
	};

	/// <summary>The zoom levels the editable zoom box offers; the box accepts anything typed too.</summary>
	public IReadOnlyList<string> ZoomLevels { get; } = new[] { "50%", "75%", "100%", "150%", "200%" };

	/// <summary>The engrave modes behind the Engrave button's menu.</summary>
	public IReadOnlyList<string> EngraveModes { get; } = new[] { "Preview", "Publish", "Custom" };

	/// <summary>The files behind the Open button's recent-files menu.</summary>
	public IReadOnlyList<string> RecentFiles { get; } = new[] { "bach-invention.ly", "satie-gnossienne.ly" };

	/// <summary>What the last command did, so the self-test and the page can both read it.</summary>
	public string LastAction
	{
		get;
		private set => SetProperty(ref field, value);
	} = string.Empty;

	/// <summary>How many commands have run.</summary>
	public int ActionCount
	{
		get;
		private set => SetProperty(ref field, value);
	}

	/// <summary>The score chosen in the inline chooser.</summary>
	public string SelectedScore
	{
		get;
		set => SetProperty(ref field, value);
	} = "Prelude in C";

	/// <summary>The zoom the editable zoom box shows; free text, because the box is editable.</summary>
	public string Zoom
	{
		get;
		set => SetProperty(ref field, value ?? string.Empty);
	} = "100%";

	/// <summary>The page the pager is on.</summary>
	public int PageNumber
	{
		get;
		set
		{
			SetProperty(ref field, Math.Clamp(value, 1, PageCount));
			NotifyPropertyChanged(nameof(PageLabel));
			PreviousPageCommand?.RaiseCanExecuteChanged();
			NextPageCommand?.RaiseCanExecuteChanged();
		}
	} = 1;

	/// <summary>How many pages the score has.</summary>
	public int PageCount { get; } = 4;

	/// <summary>What the pager shows between its two buttons.</summary>
	public string PageLabel => $"{PageNumber} / {PageCount}";

	/// <summary>Whether the magnifier is switched on; the toggle button binds two-way to this.</summary>
	public bool IsMagnifierOn
	{
		get;
		set => SetProperty(ref field, value);
	}

	/// <summary>Whether Print can run; the demo flips it to show IsEnabled following CanExecute.</summary>
	public bool CanPrint
	{
		get;
		set
		{
			SetProperty(ref field, value);
			PrintCommand?.RaiseCanExecuteChanged();
		}
	} = true;

	/// <summary>New score. A XamlUICommand, so it carries its own label, icon and shortcut.</summary>
	public XamlUICommand NewCommand { get; }

	/// <summary>Open a score. A XamlUICommand.</summary>
	public XamlUICommand OpenCommand { get; }

	/// <summary>Save the score. A XamlUICommand.</summary>
	public XamlUICommand SaveCommand { get; }

	/// <summary>Save the score under a new name. A plain command behind the Save button's arrow.</summary>
	public ICommand SaveAsCommand { get; }

	/// <summary>Print. A plain command that can say no, which disables its button.</summary>
	public SimpleCommand PrintCommand { get; }

	/// <summary>Engrave the score. A plain command; its button's arrow opens the modes.</summary>
	public ICommand EngraveCommand { get; }

	/// <summary>Open one of the recent files; the menu item passes the file name.</summary>
	public ICommand OpenRecentCommand { get; }

	/// <summary>Engrave in one particular mode; the menu item passes the mode.</summary>
	public ICommand EngraveModeCommand { get; }

	/// <summary>Go back a page. Says no on the first page.</summary>
	public SimpleCommand PreviousPageCommand { get; }

	/// <summary>Go on a page. Says no on the last page.</summary>
	public SimpleCommand NextPageCommand { get; }

	private void Record(string what)
	{
		LastAction = what;
		ActionCount++;
	}

	/// <summary>
	/// Builds a <c>XamlUICommand</c> carrying its label, description, icon and keyboard accelerator.
	/// </summary>
	/// <param name="label">The command's label, which becomes the button's text.</param>
	/// <param name="description">The command's description, which the button's tooltip includes.</param>
	/// <param name="icon">The name of the icon in the demo's set.</param>
	/// <param name="key">The accelerator's key.</param>
	/// <param name="modifiers">The accelerator's modifiers.</param>
	/// <returns>The command.</returns>
	private static XamlUICommand UiCommand(
		string label, string description, string icon, VirtualKey key, VirtualKeyModifiers modifiers)
	{
		var command = new XamlUICommand
		{
			Label = label,
			Description = description,
			IconSource = new SvgIconSource
			{
				Source = DemoIcons.Light(icon),
				Dark = DemoIcons.Dark(icon),
			},
		};

		command.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = key, Modifiers = modifiers });

		return command;
	}
}
