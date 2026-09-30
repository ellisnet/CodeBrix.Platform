using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayTestDemo.ViewModels;

[Microsoft.UI.Xaml.Data.Bindable]
public sealed class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public int CheckChanges { get; private set; }
    public int ToggleChanges { get; private set; }
    public int ScrollClicks { get; private set; }
    public int PickerOperations { get; private set; }
    public string CheckStatus { get; private set; } = "Unchecked · 0 changes";
    public string ToggleStatus { get; private set; } = "Off · 0 changes";
    public string ScrollStatus { get; private set; } = "The target has not been clicked.";
    public string PickerStatus { get; private set; } = "Choose a file or folder to see its path here.";
    public string PickerOutcome { get; private set; } = "Ready";
    public string[] SelectedPaths { get; private set; } = Array.Empty<string>();
    public string SelectedText { get; private set; } = "";
    public Exception LastPickerError { get; private set; }

    public void RecordCheck(bool value)
    {
        CheckChanges++;
        CheckStatus = $"{(value ? "Checked" : "Unchecked")} · {CheckChanges} changes";
        Changed(nameof(CheckStatus));
    }

    public void RecordToggle(bool value)
    {
        ToggleChanges++;
        ToggleStatus = $"{(value ? "On" : "Off")} · {ToggleChanges} changes";
        Changed(nameof(ToggleStatus));
    }

    public void RecordScrollClick()
    {
        ScrollClicks++;
        ScrollStatus = $"Target clicked {ScrollClicks} time(s).";
        Changed(nameof(ScrollStatus));
    }

    public void RecordPicker(string[] paths, string text, Exception error)
    {
        SelectedPaths = paths ?? Array.Empty<string>();
        SelectedText = text ?? "";
        LastPickerError = error;
        PickerOutcome = error != null ? "Error" : SelectedPaths.Length == 0 ? "Cancelled" : "Selected";
        PickerStatus = error != null ? $"{error.GetType().Name}: {error.Message}"
            : SelectedPaths.Length == 0 ? "Selection cancelled."
            : string.Join(Environment.NewLine, SelectedPaths);
        PickerOperations++;
        Changed(nameof(PickerStatus));
        Changed(nameof(PickerOutcome));
        Changed(nameof(SelectedText));
    }

    private void Changed([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
