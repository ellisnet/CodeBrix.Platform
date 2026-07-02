using CodeBrix.Platform.Simple;
using JustBetweenUs.ViewModels;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;

// ReSharper disable once CheckNamespace
namespace JustBetweenUs.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        //Doing this before InitializeComponent() - in case InitializeComponent()
        //  is the thing that sets the binding context.
        BindingContextChanged += (sender, args) =>
        {
            (BindingContext as IXamlRootGetter)?.SetXamlRootGetter(() => this);

            if (BindingContext is ICopyToClipboard copy)
            {
                copy.CopyTextToClipboard = (text) =>
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        Clipboard.Default.SetTextAsync(text); //Not necessary to await this
                    }
                };
            }
        };
        InitializeComponent();
    }
}
