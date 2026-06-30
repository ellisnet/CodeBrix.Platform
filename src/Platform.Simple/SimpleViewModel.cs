using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#if (WIN_UI || HAS_CODEBRIX) //WIN_UI needs to be manually defined on Win UI projects (and CodeBrix.Platform net10.0-windows projects)
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#elif MAUI //Needs to be manually defined on .NET MAUI projects
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Devices;
#else
using System.Windows;
//IMPORTANT: If this code is being placed in a project that is not the main WPF application project - i.e. if it is placed in a
//  class library that is referenced by the WPF application - then the project must be set to use the Windows SDK for compilation,
//  with the following in the .csproj file:
/*
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
*/
//Note that this also means that any projects that reference the project containing this code, must also have the TargetFramework
//  set to 'net10.0-windows' - e.g. Tests projects that test the view models that inherit from SimpleViewModel.
//  (This is not relevant to WIN_UI or HAS_CODEBRIX or MAUI projects that include this file.)
#endif

namespace CodeBrix.Platform.Simple;

#if (WIN_UI || HAS_CODEBRIX || MAUI)
public abstract class SimpleViewModel : IXamlRootGetter, INotifyPropertyChanged, IDisposable
#else
public abstract class SimpleViewModel : INotifyPropertyChanged, IDisposable
#endif
{
    // ReSharper disable InconsistentNaming
    private static bool? _isInDesignMode;
    protected bool _isUnderTest;
    // ReSharper restore InconsistentNaming

#if (WIN_UI || HAS_CODEBRIX || MAUI)
    //Don't currently know how to check and see if the view-model instance is in "design mode" -
    //  for WinUI and .NET MAUI.
    protected static bool IsDesignMode(bool defaultValueIfNotSet) =>
        _isInDesignMode ?? defaultValueIfNotSet;
#else
    protected static bool IsDesignMode(bool? defaultValueIfNotSet = null)
    {
        if (!_isInDesignMode.HasValue)
        {
            if (defaultValueIfNotSet.HasValue)
            {
                return defaultValueIfNotSet.Value;
            }

            //Checking GetIsInDesignMode() works fine (WPF-only so far) when the application is being designed
            //  in Visual Studio; but does not work correctly when being designed in JetBrains Rider.
            _isInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
        }
        return _isInDesignMode.Value;
    }
#endif

    public static void SetIsDesignMode(bool isDesignMode) => _isInDesignMode = isDesignMode;

#if (WIN_UI || HAS_CODEBRIX)
    private DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private Func<XamlRoot> _xamlRootGetter;

    protected XamlRoot GetXamlRoot()
    {
        try
        {
            if (_xamlRootGetter == null)
            {
                throw new InvalidOperationException(
                    $"Unable to perform the requested UI operation before {nameof(SetXamlRootGetter)}() has been called.");
            }

            return _xamlRootGetter.Invoke();
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception e)
        {
            throw new InvalidOperationException("Getting XamlRoot on WinUI and CodeBrix.Platform can fail if not executed on the main thread.", e);
        }
    }

    //Example of how this SetXamlRootGetter() method should be used in the constructor of a
    //  Microsoft.UI.Xaml (Win UI) code-behind file:

    //1) The root element in the XAML file should have a name - since the DataContext is set at
    //  the root element level (not at the Window level) in Win UI - like this:
    //  <Grid x:Name="RootUiElement">
    //    <Grid.DataContext>
    //      <vm:MainViewModel />
    //    </Grid.DataContext>
    //    [...rest of page...]
    //  </Grid>

    //2) The constructor of the code-behind file for the XAML window should have this
    //  (after the InitializeComponent() call):
    //    (this.RootUiElement.DataContext as IXamlRootGetter)?
    //      .SetXamlRootGetter(() => this.RootUiElement.XamlRoot);

    public void SetXamlRootGetter(Func<XamlRoot> getter) => _xamlRootGetter = getter
        ?? throw new ArgumentNullException(nameof(getter));
#endif

#if MAUI
    private Func<Page> _xamlRootGetter;

    protected Page GetXamlRoot()
    {
        if (_xamlRootGetter == null)
        {
            throw new InvalidOperationException(
                $"Unable to perform the requested UI operation before {nameof(SetXamlRootGetter)}() has been called.");
        }

        return _xamlRootGetter.Invoke();
    }

    //Example of how this SetXamlRootGetter() method should be called from the code-behind file
    //  of a .NET MAUI XAML Page (e.g. ContentPage):

    //1) The Page should have the BindingContext set to an instance of the viewmodel, like this:
    //  (with the 'vm' namespace set as: xmlns:vm="clr-namespace:MyApplication.ViewModels" )
    //  <ContentPage.BindingContext>
    //    <vm:MainViewModel />
    //  </ContentPage.BindingContext>

    //2) Override OnBindingContextChanged() in the code-behind file for the Page as:
    //  protected override void OnBindingContextChanged()
    //  {
    //    base.OnBindingContextChanged();
    //    (BindingContext as IXamlRootGetter)?.SetXamlRootGetter(() => this);
    //  }

    public void SetXamlRootGetter(Func<Page> getter) => _xamlRootGetter = getter
        ?? throw new ArgumentNullException(nameof(getter));
#endif

    protected static T GetService<T>() where T : class => SimpleServiceResolver.Instance.GetService<T>();
    protected static IEnumerable<T> GetServices<T>() where T : class => SimpleServiceResolver.Instance.GetServices<T>();
    protected static void MessagingSend<TSender, TArgs>(TSender sender, string message, TArgs args)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).Send(sender, message, args);

    protected static void MessagingSend<TSender>(TSender sender, string message)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).Send(sender, message);

    protected static void MessagingSubscribe<TSender, TArgs>(
        object subscriber,
        string message,
        Action<TSender, TArgs> callback,
        TSender source)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).Subscribe(subscriber, message, callback, source);

    protected static void MessagingSubscribeFrom<TSender>(
        object subscriber,
        string message,
        Action<TSender> callback,
        TSender source) where TSender : class =>
        (GetService<ISimpleMessaging>()).SubscribeFrom(subscriber, message, callback, source);

    protected static void MessagingSubscribe<TArgs>(
        object subscriber,
        string message,
        Action<TArgs> callback) =>
        (GetService<ISimpleMessaging>()).Subscribe(subscriber, message, callback);

    protected static void MessagingSubscribe<TSender, TArgs>(
        object subscriber,
        string message,
        Func<TSender, TArgs, Task> callback,
        TSender source)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).Subscribe(subscriber, message, callback, source);

    protected static void MessagingSubscribeFrom<TSender>(
        object subscriber,
        string message,
        Func<TSender, Task> callback,
        TSender source) where TSender : class =>
        (GetService<ISimpleMessaging>()).SubscribeFrom(subscriber, message, callback, source);

    protected static void MessagingSubscribe<TArgs>(
        object subscriber,
        string message,
        Func<TArgs, Task> callback) =>
        (GetService<ISimpleMessaging>()).Subscribe(subscriber, message, callback);

    protected static void MessagingUnsubscribe<TSender, TArgs>(object subscriber, string message)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).Unsubscribe<TSender, TArgs>(subscriber, message);

    protected static void MessagingUnsubscribeFrom<TSender>(object subscriber, string message)
        where TSender : class =>
        (GetService<ISimpleMessaging>()).UnsubscribeFrom<TSender>(subscriber, message);

    protected static void MessagingUnsubscribe<TArgs>(object subscriber, string message) =>
        (GetService<ISimpleMessaging>()).Unsubscribe<TArgs>(subscriber, message);

    #region | Dialog helpers |

#if (WIN_UI || HAS_CODEBRIX)
    protected virtual SimpleDialog CreateDialog(
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        SimpleDialog.Create(_xamlRootGetter, _dispatcher, message, title, buttons);
#elif (MAUI)
    protected virtual SimpleDialog CreateDialog(
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        SimpleDialog.Create(_xamlRootGetter, message, title, buttons);
#else
    protected virtual SimpleDialog CreateDialog(
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        SimpleDialog.Create(message, title, buttons);
#endif

    protected virtual async Task ShowInfo(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            using var dialog = CreateDialog(message, "Information");
            _ = await dialog.ShowAsync();
        }
    }

    protected virtual async Task ShowError(string message, string details = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            message = $"An error occurred:\n   {message.Trim()}";
            details = (string.IsNullOrWhiteSpace(details))
                ? ""
                : (details.Trim().Length > 200)
                    ? $"{details.Trim()[..195].Trim()}[...]"
                    : details.Trim();
            message += (details == "")
                ? ""
                : $"\n\nDetails:\n{details}";
            using var dialog = CreateDialog(message, "ERROR");
            _ = await dialog.ShowAsync();
        }
    }

    protected virtual async Task ShowError(Exception exception, string message = null)
    {
        if (exception != null)
        {
            var msg = (string.IsNullOrWhiteSpace(message))
                ? exception.Message
                : $"{message.Trim()} - {exception.Message}";
            await ShowError(msg, exception.ToString());
        }
    }

    protected virtual async Task<bool> ConfirmDialog(
        string message,
        string title = "Are you sure?",
        SimpleDialogButtons confirmButtons = SimpleDialogButtons.YesNo)
    {
        title = (string.IsNullOrWhiteSpace(title))
            ? "Are you sure?"
            : title.Trim();

        message = (string.IsNullOrWhiteSpace(message))
            ? title
            : message.Trim();

        var confirmResult = (confirmButtons == SimpleDialogButtons.YesNo)
            ? SimpleDialogResult.Yes
            : SimpleDialogResult.OK;

        using var confirm = CreateDialog(message, title, confirmButtons);
        return (await confirm.ShowAsync()) == confirmResult;
    }

    #endregion

#if (WIN_UI || HAS_CODEBRIX)

    protected static Visibility GetVisibility(bool isVisible)
    {
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    protected virtual void InvokeOnMainThread(Action functionToExecute) =>
        _dispatcher.InvokeOnMainThread(functionToExecute);

    protected virtual Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> functionToExecute) =>
        _dispatcher.InvokeOnMainThreadAsync(functionToExecute);

#elif MAUI

    protected static Visibility GetVisibility(bool isVisible)
    {
        return isVisible ? Visibility.Visible : Visibility.Hidden;
    }

    protected virtual void InvokeOnMainThread(Action functionToExecute)
    {
        if (functionToExecute != null)
        {
            if (MainThread.IsMainThread)
            {
                functionToExecute.Invoke();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(functionToExecute.Invoke);
            }
        }
    }

    protected virtual async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> functionToExecute)
    {
        T result = default;

        if (functionToExecute != null)
        {
            if (MainThread.IsMainThread)
            {
                result = await functionToExecute.Invoke();
            }
            else
            {
                result = await MainThread.InvokeOnMainThreadAsync(functionToExecute);
            }
        }

        return result;
    }

#else

    protected static Visibility GetVisibility(bool isVisible) => (isVisible)
        ? Visibility.Visible
        : Visibility.Hidden;

    protected virtual void InvokeOnMainThread(Action functionToExecute)
    {
        if (functionToExecute != null)
        {
            if (_isUnderTest && Application.Current?.Dispatcher == null)
            {
                functionToExecute.Invoke();
            }
            else
            {
                // ReSharper disable once PossibleNullReferenceException
                Application.Current.Dispatcher.Invoke(functionToExecute.Invoke);
            }
        }
    }

    protected virtual Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> functionToExecute)
    {
        var completionSource = new TaskCompletionSource<T>();

        if (_isUnderTest && Application.Current?.Dispatcher == null)
        {
            // ReSharper disable once AsyncVoidLambda
            new Task(async () =>
            {
                try
                {
                    T result = await functionToExecute.Invoke();
                    completionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
            }).Start();
        }
        else
        {
            // ReSharper disable once PossibleNullReferenceException
            Application.Current.Dispatcher.Invoke(async () =>
            {
                try
                {
                    T result = await functionToExecute.Invoke();
                    completionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
            });
        }

        return completionSource.Task;
    }

#endif

    #region | INotifyPropertyChanged implementation and helper methods |

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void NotifyPropertyChanged(string propertyName, bool notifyOnMainThread = false)
    {
        if ((!string.IsNullOrWhiteSpace(propertyName)))
        {
            if (notifyOnMainThread)
            {
                InvokeOnMainThread(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                    CheckAffectedProperties(propertyName);
                    CheckAffectedCommands(propertyName);
                });
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                CheckAffectedProperties(propertyName);
                CheckAffectedCommands(propertyName);
            }
        }
    }

    protected virtual void ThisPropertyChanged([CallerMemberName] string propertyName = "", bool notifyOnMainThread = false) =>
        NotifyPropertyChanged(propertyName, notifyOnMainThread);

    protected virtual void RaiseCanExecuteChanged(SimpleCommand command) =>
        command?.RaiseCanExecuteChanged();

    protected virtual void CheckAffectedProperties(string propertyName)
    {
        if (!string.IsNullOrWhiteSpace(propertyName))
        {
            var propInfo = GetType().GetProperties()
                .FirstOrDefault(f => f.Name.Equals(propertyName.Trim(),
                    StringComparison.InvariantCultureIgnoreCase));
            if (propInfo != null)
            {
                var attrib = propInfo
                    .GetCustomAttributes<AffectsPropertiesAttribute>(true)
                    .FirstOrDefault();

                if (attrib != null)
                {
                    foreach (var affected in attrib.AffectedProperties)
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(affected));
                    }
                }
            }
        }
    }

    protected virtual void CheckAffectedCommands(string propertyName)
    {
        if (!string.IsNullOrWhiteSpace(propertyName))
        {
            var propInfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var propInfo = propInfos.FirstOrDefault(f => f.Name.Equals(propertyName.Trim(),
                StringComparison.InvariantCultureIgnoreCase));
            if (propInfo != null)
            {
                //1 - Check for AffectsAllCommandsAttribute
                var affectsAll = propInfo
                    .GetCustomAttributes<AffectsAllCommandsAttribute>(true)
                    .FirstOrDefault();

                if (affectsAll != null)
                {
                    foreach (var cmdPropInfo in propInfos.Where(w => w.PropertyType == typeof(SimpleCommand)))
                    {
                        (cmdPropInfo.GetValue(this) as SimpleCommand)?.RaiseCanExecuteChanged();
                    }
                }
                else
                {
                    //2 - Check for AffectsCommandsAttribute
                    var affectsCommands = propInfo
                        .GetCustomAttributes<AffectsCommandsAttribute>(true)
                        .FirstOrDefault();

                    if (affectsCommands != null)
                    {
                        foreach (var affected in affectsCommands.AffectedCommands)
                        {
                            var cmdPropInfo = propInfos.FirstOrDefault(f => f.Name.Equals(affected,
                                StringComparison.InvariantCultureIgnoreCase));
                            (cmdPropInfo?.GetValue(this) as SimpleCommand)?.RaiseCanExecuteChanged();
                        }
                    }
                }
            }
        }
    }

    protected virtual void SetProperty<T>(ref T property, T newValue,
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false)
        where T : class
    {
        if ((property == null && newValue != null)
            || (property != null && (newValue == null || (!property.Equals(newValue)))))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName, notifyOnMainThread);
        }
    }

    protected virtual void SetEnumProperty<TEnum>(ref TEnum property, TEnum newValue,
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false)
        where TEnum : Enum
    {
        if (Enum.IsDefined(typeof(TEnum), property)
            && Enum.IsDefined(typeof(TEnum), newValue)
            && (!property.Equals(newValue)))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName, notifyOnMainThread);
        }
    }

    protected virtual void SetProperty(ref string property, string newValue,
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false)
    {
        if ((property == null && newValue != null)
            || (property != null && (newValue == null || (!property.Equals(newValue)))))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName, notifyOnMainThread);
        }
    }

    protected virtual void SetProperty(ref bool property, bool newValue,
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false)
    {
        if (!property.Equals(newValue))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName, notifyOnMainThread);
        }
    }

    protected virtual void SetProperty(ref DateTime property, DateTime newValue,
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false)
    {
        if (!property.Equals(newValue))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName, notifyOnMainThread);
        }
    }

    protected virtual void SetProperty(ref DateTimeOffset property, DateTimeOffset newValue,
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false)
    {
        if (!property.Equals(newValue))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName, notifyOnMainThread);
        }
    }

    protected virtual void SetProperty(ref int property, int newValue,
        [CallerMemberName] string propertyName = "", bool notifyOnMainThread = false)
    {
        if (!property.Equals(newValue))
        {
            property = newValue;
            NotifyPropertyChanged(propertyName, notifyOnMainThread);
        }
    }

    #endregion

    #region | IDisposable implementation |

    protected virtual void CheckDisposed(Func<bool> disposedChecker, [CallerMemberName] string caller = null)
    {
        if (disposedChecker?.Invoke() ?? false)
        {
            throw new ObjectDisposedException(objectName: GetType().Name,
                message: (string.IsNullOrWhiteSpace(caller))
                ? $"This {GetType().Name} instance has been disposed."
                : $"Cannot call {caller.Trim()} on a {GetType().Name} instance that has been disposed.");
        }
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // remove event handlers before setting event to null
            Delegate[] delegates = PropertyChanged?.GetInvocationList();
            if (delegates != null)
            {
                foreach (var d in delegates)
                {
                    PropertyChanged -= (PropertyChangedEventHandler)d;
                }
            }
            PropertyChanged = null;

#if (WIN_UI || HAS_CODEBRIX)
            _dispatcher = null;
#endif

#if (WIN_UI || HAS_CODEBRIX || MAUI)
            _xamlRootGetter = null;
#endif
        }
    }

    #endregion
}
