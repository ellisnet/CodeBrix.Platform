#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

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

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AffectsPropertiesAttribute : Attribute
{
    public IList<string> AffectedProperties { get; }

    public AffectsPropertiesAttribute(params string[] propertyNames) =>
        AffectedProperties = (propertyNames ?? [])
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(s => s.Trim())
            .ToArray();
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AffectsCommandsAttribute : Attribute
{
    public IList<string> AffectedCommands { get; }

    public AffectsCommandsAttribute(params string[] commandNames) =>
        AffectedCommands = (commandNames ?? [])
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(s => s.Trim())
            .ToArray();
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AffectsAllCommandsAttribute : Attribute;

public class SimpleCommand : ICommand, IDisposable
{
#if (WIN_UI || HAS_CODEBRIX)
    private DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private DispatcherQueue GetDispatcher()
    {
        _dispatcher ??= DispatcherQueue.GetForCurrentThread();
        return _dispatcher;
    }
#endif

    private enum ExecuteStyle
    {
        None = 0,
        SyncWithParam,
        SyncNoParam,
        AsyncWithParam,
        AsyncNoParam,
    }

    private readonly ExecuteStyle _executeStyle;

    //Sync execution styles
    private Action<object> _executeWithParamSync; //allows passing an object parameter to function
    private Action _executeNoParamSync; //no parameter passing

    //Async execution styles
    private Func<object, Task> _executeWithParamAsync; //allows passing an object parameter to function
    private Func<Task> _executeNoParamAsync; //no parameter passing

    private Func<object, bool> _canExecuteWithParam; //allows passing an object parameter to function
    private Func<bool> _canExecuteNoParam; //no parameter passing

    public bool ShouldExecuteOnMainThread { get; set; }
    public bool ShouldRaiseCanExecuteOnMainThread { get; set; } = true;

    private SimpleCommand(
        Func<object, bool> canExecuteWithParam, Action<object> executeWithParamSync, Func<object, Task> executeWithParamAsync,
        Func<bool> canExecuteNoParam, Action executeNoParamSync, Func<Task> executeNoParamAsync,
        bool shouldExecuteOnMainThread, ExecuteStyle executeStyle)
    {
        _canExecuteWithParam = canExecuteWithParam;
        _executeWithParamSync = executeWithParamSync;
        _executeWithParamAsync = executeWithParamAsync;
        _canExecuteNoParam = canExecuteNoParam;
        _executeNoParamSync = executeNoParamSync;
        _executeNoParamAsync = executeNoParamAsync;
        ShouldExecuteOnMainThread = shouldExecuteOnMainThread;
        _executeStyle = executeStyle;
    }

    private bool IsExecutable => Enum.IsDefined(_executeStyle)
                                   && (_executeStyle != ExecuteStyle.None)
                                   && ((_executeStyle == ExecuteStyle.SyncWithParam && _executeWithParamSync != null)
                                       || (_executeStyle == ExecuteStyle.AsyncWithParam && _executeWithParamAsync != null)
                                       || (_executeStyle == ExecuteStyle.SyncNoParam && _executeNoParamSync != null)
                                       || (_executeStyle == ExecuteStyle.AsyncNoParam && _executeNoParamAsync != null));

    #region | SyncWithParam style constructors |

    public SimpleCommand(Func<object, bool> canExecuteFunction, Action<object> executeFunction,
        bool executeOnMainThread = false)
        : this(canExecuteFunction, executeFunction,
            null, null, null, null,
            executeOnMainThread, ExecuteStyle.SyncWithParam)
    { }

    public SimpleCommand(Func<bool> canExecuteFunction, Action<object> executeFunction,
        bool executeOnMainThread = false)
        : this(null, executeFunction, null,
            canExecuteFunction, null, null,
            executeOnMainThread, ExecuteStyle.SyncWithParam)
    { }

    public SimpleCommand(Action<object> executeFunction, bool executeOnMainThread = false)
        : this(null, executeFunction, null,
            null, null, null,
            executeOnMainThread, ExecuteStyle.SyncWithParam)
    { }

    #endregion

    #region | SyncNoParam style constructors |

    public SimpleCommand(Func<object, bool> canExecuteFunction, Action executeFunction,
        bool executeOnMainThread = false)
        : this(canExecuteFunction, null, null,
            null, executeFunction, null,
            executeOnMainThread, ExecuteStyle.SyncNoParam)
    { }

    public SimpleCommand(Func<bool> canExecuteFunction, Action executeFunction, bool executeOnMainThread = false)
        : this(null, null, null,
            canExecuteFunction, executeFunction, null,
            executeOnMainThread, ExecuteStyle.SyncNoParam)
    { }

    public SimpleCommand(Action executeFunction, bool executeOnMainThread = false)
        : this(null, null, null,
            null, executeFunction, null,
            executeOnMainThread, ExecuteStyle.SyncNoParam)
    { }

    #endregion

    #region | AsyncWithParam style constructors |

    public SimpleCommand(Func<object, bool> canExecuteFunction, Func<object, Task> executeFunction,
        bool executeOnMainThread = false)
        : this(canExecuteFunction, null, executeFunction,
            null, null, null,
            executeOnMainThread, ExecuteStyle.AsyncWithParam)
    { }

    public SimpleCommand(Func<bool> canExecuteFunction, Func<object, Task> executeFunction,
        bool executeOnMainThread = false)
        : this(null, null, executeFunction,
            canExecuteFunction, null, null,
            executeOnMainThread, ExecuteStyle.AsyncWithParam)
    { }

    public SimpleCommand(Func<object, Task> executeFunction, bool executeOnMainThread = false)
        : this(null, null, executeFunction,
            null, null, null,
            executeOnMainThread, ExecuteStyle.AsyncWithParam)
    { }

    #endregion

    #region | AsyncNoParam style constructors |

    public SimpleCommand(Func<object, bool> canExecuteFunction, Func<Task> executeFunction,
        bool executeOnMainThread = false)
        : this(canExecuteFunction, null, null,
            null, null, executeFunction,
            executeOnMainThread, ExecuteStyle.AsyncNoParam)
    { }

    public SimpleCommand(Func<bool> canExecuteFunction, Func<Task> executeFunction, bool executeOnMainThread = false)
        : this(null, null, null,
            canExecuteFunction, null, executeFunction,
            executeOnMainThread, ExecuteStyle.AsyncNoParam)
    { }

    public SimpleCommand(Func<Task> executeFunction, bool executeOnMainThread = false)
        : this(null, null, null,
            null, null, executeFunction,
            executeOnMainThread, ExecuteStyle.AsyncNoParam)
    { }

    #endregion

    public bool CanExecute(object parameter)
    {
        var result = false;

        if (IsExecutable && _canExecuteWithParam != null)
        {
            result = _canExecuteWithParam.Invoke(parameter);
        }
        else if (IsExecutable && _canExecuteNoParam != null)
        {
            result = _canExecuteNoParam.Invoke();
        }

        return result;
    }

    // ReSharper disable AsyncVoidMethod

    private static async void WaitForExecute(TaskCompletionSource tsc, Func<object, Task> execute, object parameter)
    {
        ArgumentNullException.ThrowIfNull(tsc, nameof(tsc));
        ArgumentNullException.ThrowIfNull(execute, nameof(execute));

        await execute.Invoke(parameter);
        tsc.SetResult();
    }

    private static async void WaitForExecute(TaskCompletionSource tsc, Func<Task> execute)
    {
        ArgumentNullException.ThrowIfNull(tsc, nameof(tsc));
        ArgumentNullException.ThrowIfNull(execute, nameof(execute));

        await execute.Invoke();
        tsc.SetResult();
    }

    public async void Execute(object parameter)
    {
        if (IsExecutable)
        {
            var executeOnMain = ShouldExecuteOnMainThread;

#if MAUI
            //Check to see if we are already on the main thread
            executeOnMain = executeOnMain && (!MainThread.IsMainThread);
#endif

            switch (_executeStyle)
            {
                case ExecuteStyle.SyncWithParam:
                    if (executeOnMain)
                    {
#if (WIN_UI || HAS_CODEBRIX)
                        var dispatcher = GetDispatcher();
                        if (dispatcher != null)
                        {
                            dispatcher.TryEnqueue(() => { _executeWithParamSync.Invoke(parameter); });
                        }
                        else
                        {
                            //dispatcher not available, try executing directly
                            _executeWithParamSync.Invoke(parameter);
                        }
#elif MAUI
                        await MainThread.InvokeOnMainThreadAsync(() => { _executeWithParamSync.Invoke(parameter); });
#else
                        Application.Current.Dispatcher.Invoke(() => { _executeWithParamSync.Invoke(parameter); });
#endif
                    }
                    else
                    {
                        _executeWithParamSync.Invoke(parameter);
                    }
                    break;

                case ExecuteStyle.SyncNoParam:
                    if (executeOnMain)
                    {
#if (WIN_UI || HAS_CODEBRIX)
                        var dispatcher = GetDispatcher();
                        if (dispatcher != null)
                        {
                            dispatcher.TryEnqueue(_executeNoParamSync.Invoke);
                        }
                        else
                        {
                            //dispatcher not available, try executing directly
                            _executeNoParamSync.Invoke();
                        }
#elif MAUI
                        await MainThread.InvokeOnMainThreadAsync(() => { _executeNoParamSync.Invoke(); });
#else
                        Application.Current.Dispatcher.Invoke(_executeNoParamSync.Invoke);
#endif
                    }
                    else
                    {
                        _executeNoParamSync.Invoke();
                    }
                    break;

                case ExecuteStyle.AsyncWithParam:
                    if (executeOnMain)
                    {
#if (WIN_UI || HAS_CODEBRIX)
                        var dispatcher = GetDispatcher();
                        if (dispatcher != null)
                        {
                            var tsc = new TaskCompletionSource();
                            var queued = dispatcher.TryEnqueue(() => { WaitForExecute(tsc, _executeWithParamAsync, parameter); });
                            if (queued)
                            {
                                await tsc.Task;
                            }
                        }
                        else
                        {
                            //dispatcher not available, try executing directly
                            await _executeWithParamAsync.Invoke(parameter);
                        }
#elif MAUI
                        await MainThread.InvokeOnMainThreadAsync(async () => { await _executeWithParamAsync.Invoke(parameter); });
#else
                        await Application.Current.Dispatcher.Invoke(async () => { await _executeWithParamAsync.Invoke(parameter); });
#endif
                    }
                    else
                    {
                        await _executeWithParamAsync.Invoke(parameter);
                    }
                    break;

                case ExecuteStyle.AsyncNoParam:
                    if (executeOnMain)
                    {
#if (WIN_UI || HAS_CODEBRIX)
                        var dispatcher = GetDispatcher();
                        if (dispatcher != null)
                        {
                            var tsc = new TaskCompletionSource();
                            var queued = dispatcher.TryEnqueue(() => { WaitForExecute(tsc, _executeNoParamAsync); });
                            if (queued)
                            {
                                await tsc.Task;
                            }
                        }
                        else
                        {
                            //dispatcher not available, try executing directly
                            await _executeNoParamAsync.Invoke();
                        }
#elif MAUI
                        await MainThread.InvokeOnMainThreadAsync(async () => { await _executeNoParamAsync.Invoke(); });
#else
                        await Application.Current.Dispatcher.Invoke(async () => { await _executeNoParamAsync.Invoke(); });
#endif
                    }
                    else
                    {
                        await _executeNoParamAsync.Invoke();
                    }
                    break;
            }
        }
    }

#if MAUI
    public async void RaiseCanExecuteChanged()
    {
        if (CanExecuteChanged != null)
        {
            if (ShouldRaiseCanExecuteOnMainThread && (!MainThread.IsMainThread))
            {
                await MainThread.InvokeOnMainThreadAsync(() => { CanExecuteChanged.Invoke(this, EventArgs.Empty); });
            }
            else
            {
                CanExecuteChanged.Invoke(this, EventArgs.Empty);
            }
        }
    }
#else
    public void RaiseCanExecuteChanged()
    {
        if (CanExecuteChanged != null)
        {
            if (ShouldRaiseCanExecuteOnMainThread)
            {
#if (WIN_UI || HAS_CODEBRIX)
                var dispatcher = GetDispatcher();
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(() => { CanExecuteChanged.Invoke(this, EventArgs.Empty); });
                }
                else
                {
                    //dispatcher not available, try executing directly
                    CanExecuteChanged.Invoke(this, EventArgs.Empty);
                }
#else
                Application.Current.Dispatcher.Invoke(() => { CanExecuteChanged.Invoke(this, EventArgs.Empty); });
#endif
            }
            else
            {
                CanExecuteChanged.Invoke(this, EventArgs.Empty);
            }
        }
    }
#endif

    // ReSharper restore AsyncVoidMethod

    public event EventHandler CanExecuteChanged;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // remove event handlers before setting event to null
            Delegate[] delegates = CanExecuteChanged?.GetInvocationList();
            if (delegates != null)
            {
                foreach (var d in delegates)
                {
                    CanExecuteChanged -= (EventHandler)d;
                }
            }
            CanExecuteChanged = null;

            _canExecuteWithParam = null;
            _executeWithParamSync = null;
            _executeWithParamAsync = null;
            _canExecuteNoParam = null;
            _executeNoParamSync = null;
            _executeNoParamAsync = null;

#if (WIN_UI || HAS_CODEBRIX)
            _dispatcher = null;
#endif
        }
    }
}
