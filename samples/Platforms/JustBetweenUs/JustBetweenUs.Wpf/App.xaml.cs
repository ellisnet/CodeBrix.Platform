using CodeBrix.Platform.Simple;
using JustBetweenUs.Encryption;
using JustBetweenUs.Helpers;
using System.Windows;

namespace JustBetweenUs;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
        {
            //Register my custom services here
            services.AddEncryption();
        });
        SimpleViewModel.SetIsDesignMode(false);
    }
}
