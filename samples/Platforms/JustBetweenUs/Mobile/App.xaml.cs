using CodeBrix.Platform.Simple;
using JustBetweenUs.Encryption;
using JustBetweenUs.Helpers;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

// ReSharper disable RedundantExtendsListEntry

namespace JustBetweenUs.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        SimpleServiceResolver.CreateInstance(HostHelper.GetHost(), services =>
        {
            //Register my custom services here
            services.AddEncryption();
        });
        SimpleViewModel.SetIsDesignMode(false);
        //MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        return new Window(new AppShell());
    }
}
