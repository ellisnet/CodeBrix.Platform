using AdvancedTextEditDemo.CodeCompletion;
using AdvancedTextEditDemo.Folding;
using AdvancedTextEditDemo.ViewModels;
using CodeBrix.Platform.Simple;
using CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;
using CodeBrix.Platform.UI.AdvancedTextEdit.Folding;
using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;
using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;
using CodeBrix.Platform.UI.AdvancedTextEdit.Indentation;
using CodeBrix.Platform.UI.AdvancedTextEdit.Indentation.CSharp;
using CodeBrix.Platform.UI.AdvancedTextEdit.Search;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Windows.Storage.Pickers;

namespace AdvancedTextEditDemo.Views;

public sealed partial class MainPage : Page
{
    private const string WelcomeText =
        "// Welcome to the AdvancedTextEdit demo!\n" +
        "// Type '.' anywhere to open the code-completion window, press Ctrl+F to search,\n" +
        "// and use the Highlighting ComboBox above to change the language.\n" +
        "class Demo\n" +
        "{\n" +
        "\tvoid Greet()\n" +
        "\t{\n" +
        "\t\tConsole.WriteLine(\"Hello!\");\n" +
        "\t}\n" +
        "}\n";

    private static bool _customHighlightingRegistered;

    private CompletionWindow _completionWindow;
    private FoldingManager _foldingManager;
    private object _foldingStrategy;

    public MainPage()
    {
        RegisterCustomHighlighting();

        DataContextChanged += (_, _) =>
        {
            //Give the view model's SimpleDialog helpers a XamlRoot to attach dialogs to
            (DataContext as IXamlRootGetter)?.SetXamlRootGetter(() => XamlRoot);
        };

        this.InitializeComponent();

        //Editor wiring, mirroring the upstream editor sample: code completion, the in-editor
        //search panel, caret-position status, and a folding refresh every two seconds
        Editor.TextArea.TextEntering += TextArea_TextEntering;
        Editor.TextArea.TextEntered += TextArea_TextEntered;
        Editor.TextArea.Caret.PositionChanged += (_, _) => UpdateStatusText();
        SearchPanel.Install(Editor.TextArea);

        var foldingUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        foldingUpdateTimer.Tick += (_, _) => UpdateFoldings();
        foldingUpdateTimer.Start();

        Editor.Text = WelcomeText;

        //The highlighting ComboBox lists every registered definition (ToString is the
        //definition name); selecting one applies it and swaps the folding + indentation
        //strategies. C# is the initial selection.
        HighlightingCombo.ItemsSource = HighlightingManager.Instance.HighlightingDefinitions;
        HighlightingCombo.SelectedItem = HighlightingManager.Instance.GetDefinition("C#");

        //The property pane starts on Options, like the upstream sample
        PropertyTargetCombo.SelectedIndex = 2;

        UpdateStatusText();
    }

    private MainViewModel ViewModel => DataContext as MainViewModel;

    //Loads the sample .xshd definition embedded in AdvancedTextEditDemo.Core and registers it
    //as "Custom Highlighting" for .cool files (once per process; pages can be re-created)
    private static void RegisterCustomHighlighting()
    {
        if (_customHighlightingRegistered) { return; }
        _customHighlightingRegistered = true;

        IHighlightingDefinition customHighlighting;
        using (var stream = typeof(MainViewModel).Assembly
            .GetManifestResourceStream("AdvancedTextEditDemo.CustomHighlighting.xshd"))
        {
            if (stream == null)
            {
                throw new InvalidOperationException("Could not find the CustomHighlighting.xshd embedded resource");
            }
            using (var reader = XmlReader.Create(stream))
            {
                customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
        HighlightingManager.Instance.RegisterHighlighting("Custom Highlighting", [".cool"], customHighlighting);
    }

    #region | Toolbar actions |

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*");

        var file = await picker.PickSingleFileAsync();
        if (file == null) { return; }

        ViewModel.CurrentFileName = file.Path;
        Editor.Load(file.Path);

        //Select the highlighting that matches the file extension; the ComboBox handler
        //applies it, and the direct call covers the case where the selection did not change
        HighlightingCombo.SelectedItem =
            HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(file.Path));
        ApplyHighlightingFromComboBox();
    }

    private async void SaveFile_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ViewModel.CurrentFileName))
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = "Document",
                DefaultFileExtension = ".txt"
            };
            picker.FileTypeChoices.Add("Text file", new List<string> { ".txt" });

            var file = await picker.PickSaveFileAsync();
            if (file == null) { return; }

            ViewModel.CurrentFileName = file.Path;
        }
        Editor.Save(ViewModel.CurrentFileName);
    }

    private void Cut_Click(object sender, RoutedEventArgs e) => Editor.Cut();

    private void Copy_Click(object sender, RoutedEventArgs e) => Editor.Copy();

    private void Paste_Click(object sender, RoutedEventArgs e) => Editor.Paste();

    private void Delete_Click(object sender, RoutedEventArgs e) => Editor.Delete();

    private void Undo_Click(object sender, RoutedEventArgs e) => Editor.Undo();

    private void Redo_Click(object sender, RoutedEventArgs e) => Editor.Redo();

    #endregion

    #region | Toggles and highlighting |

    private void WordWrap_Toggled(object sender, RoutedEventArgs e)
    {
        if (Editor == null) { return; } //Raised while InitializeComponent is still building the page
        Editor.WordWrap = WordWrapCheck.IsChecked == true;
    }

    private void LineNumbers_Toggled(object sender, RoutedEventArgs e)
    {
        if (Editor == null) { return; } //Raised while InitializeComponent is still building the page
        Editor.ShowLineNumbers = LineNumbersCheck.IsChecked == true;
    }

    private void ShowEndOfLine_Toggled(object sender, RoutedEventArgs e)
    {
        if (Editor == null) { return; } //Raised while InitializeComponent is still building the page
        Editor.Options.ShowEndOfLine = ShowEndOfLineCheck.IsChecked == true;
    }

    private void HighlightingCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyHighlightingFromComboBox();
    }

    //Applies the ComboBox's selected syntax highlighting and swaps the folding and indentation
    //strategies the way the upstream sample does: XML gets the XML folding strategy, the
    //brace-based languages get brace folding (plus C# smart indentation), and everything else
    //gets default indentation with no folding
    private void ApplyHighlightingFromComboBox()
    {
        if (Editor == null) { return; }

        var definition = HighlightingCombo.SelectedItem as IHighlightingDefinition;
        Editor.SyntaxHighlighting = definition;

        if (definition == null)
        {
            _foldingStrategy = null;
        }
        else
        {
            switch (definition.Name)
            {
                case "XML":
                    _foldingStrategy = new XmlFoldingStrategy();
                    Editor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
                    break;
                case "C#":
                case "C++":
                case "PHP":
                case "Java":
                    Editor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(Editor.Options);
                    _foldingStrategy = new BraceFoldingStrategy();
                    break;
                default:
                    Editor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
                    _foldingStrategy = null;
                    break;
            }
        }

        if (_foldingStrategy != null)
        {
            _foldingManager ??= FoldingManager.Install(Editor.TextArea);
            UpdateFoldings();
        }
        else if (_foldingManager != null)
        {
            FoldingManager.Uninstall(_foldingManager);
            _foldingManager = null;
        }
    }

    private void UpdateFoldings()
    {
        if (_foldingManager == null) { return; }

        if (_foldingStrategy is BraceFoldingStrategy braceStrategy)
        {
            braceStrategy.UpdateFoldings(_foldingManager, Editor.Document);
        }
        if (_foldingStrategy is XmlFoldingStrategy xmlStrategy)
        {
            xmlStrategy.UpdateFoldings(_foldingManager, Editor.Document);
        }
    }

    #endregion

    #region | Code completion |

    private void TextArea_TextEntered(object sender, TextInputEventArgs e)
    {
        if (e.Text == ".")
        {
            //Open code completion after the user has pressed dot. A closed window cannot be
            //reopened, so every completion session gets a new window.
            var completionWindow = new CompletionWindow(Editor.TextArea);
            var data = completionWindow.CompletionList.CompletionData;
            data.Add(new MyCompletionData("Item1"));
            data.Add(new MyCompletionData("Item2"));
            data.Add(new MyCompletionData("Item3"));
            data.Add(new MyCompletionData("Another item"));
            completionWindow.Closed += (_, _) =>
            {
                if (_completionWindow == completionWindow) { _completionWindow = null; }
            };
            _completionWindow = completionWindow;
            completionWindow.Show();
        }
    }

    private void TextArea_TextEntering(object sender, TextInputEventArgs e)
    {
        if (e.Text.Length > 0 && _completionWindow != null)
        {
            if (!char.IsLetterOrDigit(e.Text[0]))
            {
                //Whenever a non-letter is typed while the completion window is open,
                //insert the currently selected element
                _completionWindow.CompletionList.RequestInsertion(EventArgs.Empty);
            }
        }
        //Do not set e.Handled = true - the typed character still needs to be inserted
    }

    #endregion

    #region | Property pane |

    private void PropertyTargetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Editor == null) { return; } //Raised while InitializeComponent is still building the page

        ViewModel?.ShowProperties(PropertyTargetCombo.SelectedIndex switch
        {
            0 => Editor,
            1 => Editor.TextArea,
            2 => (object)Editor.Options,
            _ => null,
        });
    }

    #endregion

    #region | Status bar |

    private void UpdateStatusText()
    {
        var viewModel = ViewModel;
        if (viewModel == null || Editor == null) { return; }

        viewModel.StatusText = $"Line {Editor.TextArea.Caret.Line}, Column {Editor.TextArea.Caret.Column}";
    }

    #endregion
}
