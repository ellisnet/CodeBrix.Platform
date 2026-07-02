using JustBetweenUs.ViewModels;
using System.Windows;

namespace JustBetweenUs.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        DataContextChanged += (sender, args) =>
        {
            if (DataContext is ICopyToClipboard copy)
            {
                copy.CopyTextToClipboard = Clipboard.SetText;
            }
        };
        InitializeComponent();
    }
}
