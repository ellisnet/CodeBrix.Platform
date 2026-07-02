#pragma warning disable CS1591

using System;
using System.Text;
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

// ReSharper disable InconsistentNaming

public enum SimpleDialogButtons
{
    OK = 0,
    OKCancel = 1,
    YesNo = 2
}

public enum SimpleDialogResult
{
    None = 0,
    OK = 1,
    Cancel = 2,
    Yes = 3,
    No = 4
}

// ReSharper restore InconsistentNaming

public class SimpleDialog : IDisposable
{
    private bool _isDisposed;

    private string _message = "";
    public string Message
    {
        get => _message;
        set => _message = (value ?? "").Trim();
    }

    private string _title;
    public string Title
    {
        get => _title;
        set => _title = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public SimpleDialogButtons Buttons { get; set; }

#if (WIN_UI || HAS_CODEBRIX)
    private SimpleDialog(
        Func<XamlRoot> xamlRootGetter,
        DispatcherQueue dispatcher,
        string message,
        string title,
        SimpleDialogButtons buttons)
    {
        _xamlRootGetter = xamlRootGetter ?? throw new ArgumentNullException(nameof(xamlRootGetter));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        Message = message;
        Title = title;
        Buttons = buttons;
    }

    public static SimpleDialog Create(
        Func<XamlRoot> xamlRootGetter,
        DispatcherQueue dispatcher,
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        new(xamlRootGetter, dispatcher, message, title, buttons);

    private static string BreakOnMaxLineLength(string text, int maxLineLength)
    {
        var result = text;

        if ((!string.IsNullOrWhiteSpace(text)) && maxLineLength > 0)
        {
            var lines = text.Replace("\r\n", "\n").Trim().Split('\n');
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (line.Length > maxLineLength)
                {
                    var pos = 0;
                    while (pos < line.Length)
                    {
                        var newLine = ((pos + maxLineLength) < line.Length)
                            ? line.Substring(pos, maxLineLength)
                            : line[pos..];
                        sb.AppendLine(newLine);
                        pos += newLine.Length;
                    }
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            result = sb.ToString().Trim();
        }

        return result;
    }

    // ReSharper disable InconsistentNaming
    //VERY IMPORTANT: With CodeBrix.Platform, anything that touches the XamlRoot needs to be running on the main thread.
    protected Func<XamlRoot> _xamlRootGetter;
    protected DispatcherQueue _dispatcher;
    // ReSharper restore InconsistentNaming
#elif MAUI
    private SimpleDialog(
        Func<Page> xamlRootGetter,
        string message,
        string title,
        SimpleDialogButtons buttons)
    {
        _xamlRootGetter = xamlRootGetter ?? throw new ArgumentNullException(nameof(xamlRootGetter));
        Message = message;
        Title = title;
        Buttons = buttons;
    }

    public static SimpleDialog Create(
        Func<Page> xamlRootGetter,
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        new(xamlRootGetter, message, title, buttons);

    // ReSharper disable InconsistentNaming
    protected Func<Page> _xamlRootGetter;
    // ReSharper restore InconsistentNaming
#else
    private SimpleDialog(
        string message,
        string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK)
    {
        Message = message;
        Title = title;
        Buttons = buttons;
    }

    public static SimpleDialog Create(string message, string title = null,
        SimpleDialogButtons buttons = SimpleDialogButtons.OK) =>
        new(message, title, buttons);
#endif

    public async Task<SimpleDialogResult> ShowAsync()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(objectName: nameof(SimpleDialog), message: "Dialog has been disposed.");
        }

        // ReSharper disable once JoinDeclarationAndInitializer
        SimpleDialogResult result;

#if (WIN_UI || HAS_CODEBRIX || MAUI)
        string firstButton;
        SimpleDialogResult firstButtonResult;
        string secondButton = null;
        SimpleDialogResult secondButtonResult = SimpleDialogResult.None;

        switch (this.Buttons)
        {
            case SimpleDialogButtons.OK:
                firstButton = "OK";
                firstButtonResult = SimpleDialogResult.OK;
                break;
            case SimpleDialogButtons.OKCancel:
                firstButton = "OK";
                firstButtonResult = SimpleDialogResult.OK;
                secondButton = "Cancel";
                secondButtonResult = SimpleDialogResult.Cancel;
                break;
            case SimpleDialogButtons.YesNo:
                firstButton = "Yes";
                firstButtonResult = SimpleDialogResult.Yes;
                secondButton = "No";
                secondButtonResult = SimpleDialogResult.No;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
#endif

#if (WIN_UI || HAS_CODEBRIX)
        result = await _dispatcher.InvokeOnMainThreadAsync(async () =>
        {
            var dlgResult = SimpleDialogResult.None;

            var dialog = new ContentDialog
            {
                Content = new TextBlock { Text = BreakOnMaxLineLength(_message, 74) },
                PrimaryButtonText = firstButton,
                IsPrimaryButtonEnabled = true,
                IsSecondaryButtonEnabled = (secondButton != null),
                XamlRoot = _xamlRootGetter.Invoke()
            };
            if (secondButton != null)
            {
                dialog.SecondaryButtonText = secondButton;
            }
            if (_title != null)
            {
                dialog.Title = _title;
            }

            var dialogResult = await dialog.ShowAsync(ContentDialogPlacement.Popup);
            if (dialogResult == ContentDialogResult.Primary)
            {
                dlgResult = firstButtonResult;
            }
            else if (dialogResult == ContentDialogResult.Secondary)
            {
                dlgResult = secondButtonResult;
            }

            return dlgResult;
        });
#elif MAUI
        if (secondButton == null)
        {
            await MainThreadHelper.SafeInvokeOnMainThreadAsync(async () =>
            {
                await _xamlRootGetter.Invoke().DisplayAlertAsync((_title ?? ""), _message, firstButton);
            });
            result = firstButtonResult;
        }
        else
        {
            result = await MainThreadHelper.SafeInvokeOnMainThreadAsync(async () => 
                (await _xamlRootGetter.Invoke().DisplayAlertAsync((_title ?? ""), _message, firstButton, secondButton)) 
                    ? firstButtonResult 
                    : secondButtonResult);
        }
#else

        var msgButton = Buttons switch
        {
            SimpleDialogButtons.OK => MessageBoxButton.OK,
            SimpleDialogButtons.OKCancel => MessageBoxButton.OKCancel,
            SimpleDialogButtons.YesNo => MessageBoxButton.YesNo,
            _ => throw new ArgumentOutOfRangeException()
        };

        var dialogResult = MessageBox.Show(_message, (_title ?? ""), msgButton);

        result = dialogResult switch
        {
            MessageBoxResult.OK => SimpleDialogResult.OK,
            MessageBoxResult.Cancel => SimpleDialogResult.Cancel,
            MessageBoxResult.Yes => SimpleDialogResult.Yes,
            MessageBoxResult.No => SimpleDialogResult.No,
            _ => SimpleDialogResult.None
        };

        //satisfy the compiler that something async is happening
        await Task.Run(() => { });

#endif

        return result;
    }

    #region | IDisposable implementation |

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isDisposed = true;
#if (WIN_UI || HAS_CODEBRIX)
            _xamlRootGetter = null;
            _dispatcher = null;
#elif (MAUI)
            _xamlRootGetter = null;
#endif
        }
    }

    #endregion
}

#if (WIN_UI || HAS_CODEBRIX)
internal static class DispatcherHelper
{
    internal static void InvokeOnMainThread(this DispatcherQueue dispatcher, Action functionToExecute)
    {
        ArgumentNullException.ThrowIfNull(dispatcher, nameof(dispatcher));

        if (functionToExecute != null)
        {
            dispatcher.TryEnqueue(functionToExecute.Invoke);
        }
    }

    internal static Task<T> InvokeOnMainThreadAsync<T>(this DispatcherQueue dispatcher, Func<Task<T>> functionToExecute)
    {
        ArgumentNullException.ThrowIfNull(dispatcher, nameof(dispatcher));
        ArgumentNullException.ThrowIfNull(functionToExecute, nameof(functionToExecute));

        var completionSource = new TaskCompletionSource<T>();

        // ReSharper disable once AsyncVoidLambda
        dispatcher.TryEnqueue(async () =>
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

        return completionSource.Task;
    }
}

public interface IXamlRootGetter
{
    public void SetXamlRootGetter(Func<XamlRoot> getter);
}
#endif

#if MAUI
internal static class MainThreadHelper
{
    internal static void SafeInvokeOnMainThread(Action functionToExecute)
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

    internal static async Task SafeInvokeOnMainThreadAsync(Func<Task> functionToExecute)
    {
        if (functionToExecute != null)
        {
            if (MainThread.IsMainThread)
            {
                await functionToExecute.Invoke();
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(functionToExecute);
            }
        }
    }

    internal static async Task<T> SafeInvokeOnMainThreadAsync<T>(Func<Task<T>> functionToExecute)
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
}

public interface IXamlRootGetter
{
    public void SetXamlRootGetter(Func<Page> getter);
}
#endif