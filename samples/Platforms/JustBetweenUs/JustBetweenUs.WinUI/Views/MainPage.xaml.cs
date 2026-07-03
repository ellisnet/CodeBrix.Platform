using CodeBrix.Platform.Simple;
using JustBetweenUs.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JustBetweenUs.WinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            //Doing this before InitializeComponent() - in case InitializeComponent()
            //  is the thing that sets the data context.
            DataContextChanged += (sender, args) =>
            {
                (DataContext as IXamlRootGetter)?.SetXamlRootGetter(() => XamlRoot);

                if (DataContext is ICopyToClipboard copy)
                {
                    copy.CopyTextToClipboard = (text) =>
                    {
                        if (!string.IsNullOrEmpty(text))
                        {
                            var clipData = new DataPackage();
                            clipData.SetText(text);
                            Clipboard.SetContent(clipData);
                        }
                    };
                }
            };

            InitializeComponent();
        }
    }
}
