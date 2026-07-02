/*
   Copyright 2025 Ellisnet - Jeremy Ellis (jeremy@ellisnet.com)
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
       http://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

//FILE DATE/REVISION: 2025-03-14

using Microsoft.Extensions.Configuration; //Required NuGet: Microsoft.Extensions.Hosting
using Microsoft.Extensions.DependencyInjection; //Required NuGet: Microsoft.Extensions.DependencyInjection
using Microsoft.Extensions.FileProviders; //Required NuGet: Microsoft.Extensions.Hosting
using Microsoft.Extensions.Hosting; //Required NuGet: Microsoft.Extensions.Hosting
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global

#pragma warning disable IDE0079
#pragma warning disable CA1050
#pragma warning disable CA1513
#pragma warning disable CA1859
#pragma warning disable IDE0290

#if SIMPLE_HTTP_FACTORY
using System.Net.Http; //Required NuGet: Microsoft.Extensions.Http - IMPORTANT: We don't need the System.Net.Http NuGet package
#endif

#if SIMPLE_OUTPUT_LOGGING
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text;
using Xunit; //Required NuGet: xunit.v3
#endif

// ReSharper disable CheckNamespace

#region | Interfaces |

public interface ITestFixture : IDisposable
{
    string EnvironmentName { get; }
    TService GetService<TService>() where TService : class;
    IEnumerable<TService> GetServices<TService>() where TService : class;

#if SIMPLE_OUTPUT_LOGGING
    void CreateAndRegisterLogger<T>(ITestOutputHelper output,
        bool registerBasicILogger = true);
#endif
}

public interface ITestingContext
{
    IHostEnvironment Host { get; }
    IConfiguration Config { get; }
}

public interface IRegisterServices
{
    Type SupportedFixture { get; }

    void RegisterServicesForTest(
        IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration config,
        Func<IServiceProvider> serviceResolver);
}

public abstract class RegisterServices<TFixture> : IRegisterServices
    where TFixture : ITestFixture
{
    public abstract void RegisterServicesForTest(
        IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration config,
        Func<IServiceProvider> serviceResolver);

    public virtual Type SupportedFixture => typeof(TFixture);
}

#if SIMPLE_OUTPUT_LOGGING

public interface IOutputLoggerFactory
{
    ILogger GetLogger(ITestOutputHelper output);
    ILogger<T> GetLogger<T>(ITestOutputHelper output);
}

#endif

#endregion

#region | Implementations |

public class SimpleTestFixture : IServiceProvider, ITestFixture
{
    protected static string DefaultEnvironmentName => nameof(Environments.Development);

#if SIMPLE_OUTPUT_LOGGING
    private Dictionary<string, ILogger> _registeredLoggers = [];
    private readonly object _loggerLocker = new();
    private IOutputLoggerFactory _loggerFactory = new SimpleOutputLoggerFactory();
#endif

    protected IServiceProvider ServiceProvider;
    protected ITestingContext Context;
    private bool _isDisposed;

    protected void CheckIsDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException($"This instance of {nameof(SimpleTestFixture)} is disposed.");
        }
    }

    protected virtual string GetEnvironmentName()
    {
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return (string.IsNullOrWhiteSpace(envName))
            ? DefaultEnvironmentName
            : envName.Trim();
    }

    protected virtual IConfiguration GetConfiguration(string environmentName) =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile($"appsettings.{environmentName}.json", true)
            .Build();

    protected virtual void RegisterCustomServices(IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration config,
        Func<IServiceProvider> serviceResolver)
    {
        //Nothing to do here, this only exists to provide a customization
        //  point for subclasses of SimpleTestFixture.
    }

    public SimpleTestFixture()
    {
        // ReSharper disable VirtualMemberCallInConstructor

        var envName = GetEnvironmentName();
        var config = GetConfiguration(envName);

        Context = new SimpleTestingContext(new SimpleHostEnvironment(envName), config);

        var services = new ServiceCollection();
        services.AddSingleton(config);
#if SIMPLE_HTTP_FACTORY
        services.AddSingleton<IHttpClientFactory, SimpleTestingHttpClientFactory>();
#endif

        RegisterCustomServices(services,
            Context.Host,
            Context.Config,
            serviceResolver: () => this);

        // ReSharper restore VirtualMemberCallInConstructor

        //Look for subclasses of RegisterServices<SimpleTestFixture> and call
        //  their RegisterServicesForTest() methods.

        foreach (var registerType in typeof(SimpleTestFixture)
                     .Assembly
                     .GetTypes()
                     .Where(w => w.IsAssignableTo(typeof(IRegisterServices))
                                 && (!w.IsInterface)))
        {
            //Want to confirm that the found IRegisterServices implementation
            //  supports the current fixture - otherwise we don't want to call
            //  RegisterServicesForTest() on it.
            var baseType = registerType.BaseType;
            if ((baseType?.IsGenericType ?? false) 
                && baseType.GetGenericArguments().Any(a => a == GetType()))
            {
                //needs an empty constructor
                var constructor = registerType.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    try
                    {
                        if (Activator.CreateInstance(registerType) is IRegisterServices instance
                            && instance.SupportedFixture == GetType())
                        {
                            instance.RegisterServicesForTest(
                                services, 
                                Context.Host, 
                                Context.Config,
                                serviceResolver: () => this);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new TypeLoadException(
                            $"Error while calling {nameof(IRegisterServices.RegisterServicesForTest)}()"
                            + $" on type: {registerType.Namespace}.{registerType.Name}"
                            , e);
                    }
                }
            }
        }

        ServiceProvider = services.BuildServiceProvider();
    }

    public IServiceProvider Services
    {
        get
        {
            CheckIsDisposed();
            return this;
        }
    }

    #region | IServiceProvider implementation |

    public object GetService(Type serviceType)
    {
        CheckIsDisposed();

#if SIMPLE_OUTPUT_LOGGING
        if (serviceType == typeof(IOutputLoggerFactory))
        {
            return _loggerFactory;
        }
        else if (serviceType == typeof(ILogger))
        {
            lock (_loggerLocker)
            {
                if (_registeredLoggers.TryGetValue(nameof(ILogger),
                        out var found))
                {
                    return found;
                }
            }
        }
        else if (serviceType.IsAssignableTo(typeof(ILogger))
                 && serviceType.IsGenericType)
        {
            lock (_loggerLocker)
            {
                var loggerType = serviceType.GetGenericArguments()
                    .FirstOrDefault(f => !f.IsGenericType);

                if (loggerType != null)
                {
                    var loggerKey = $"{nameof(ILogger)}_{loggerType.FullName}";
                    if (_registeredLoggers.TryGetValue(loggerKey, out var found))
                    {
                        return found;
                    }
                }
            }
        }
#endif

        return ServiceProvider.GetService(serviceType)
            ?? throw new InvalidOperationException("Unable to find a registered instance of type "
                + $"'{serviceType.Name}' - maybe it wasn't registered.");
    }

    #endregion

    #region | ITestFixture implementation |

    public string EnvironmentName => Context?.Host?.EnvironmentName;

    public TService GetService<TService>() where TService : class
    {
        CheckIsDisposed();
        var serviceType = typeof(TService);

        return GetService(serviceType) as TService
            ?? throw new InvalidOperationException("Unable to find a registered instance of type "
                + $"'{serviceType.Name}' - maybe it wasn't registered.");
    }

    public IEnumerable<TService> GetServices<TService>() where TService : class
    {
        CheckIsDisposed();
        return ServiceProvider.GetServices<TService>();
    }

#if SIMPLE_OUTPUT_LOGGING
    public void CreateAndRegisterLogger<T>(ITestOutputHelper output,
        bool registerBasicILogger = true)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (typeof(T).IsGenericType)
        {
            throw new InvalidOperationException("Cannot create a logger for a generic type.");
        }

        lock (_loggerLocker)
        {
            var loggerKey = $"{nameof(ILogger)}_{typeof(T).FullName}";
            if (_registeredLoggers.ContainsKey(loggerKey))
            {
                _registeredLoggers[loggerKey] = _loggerFactory.GetLogger<T>(output);
            }
            else
            {
                _registeredLoggers.Add(loggerKey, _loggerFactory.GetLogger<T>(output));
            }

            if (registerBasicILogger)
            {
                loggerKey = nameof(ILogger);
                if (_registeredLoggers.ContainsKey(loggerKey))
                {
                    _registeredLoggers[loggerKey] = _loggerFactory.GetLogger(output);
                }
                else
                {
                    _registeredLoggers.Add(loggerKey, _loggerFactory.GetLogger(output));
                }
            }
        }
    }
#endif

    #endregion

    #region | IDisposable implementation |

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && (!_isDisposed))
        {
            _isDisposed = true;
            ServiceProvider = null;
            Context = null;
#if SIMPLE_OUTPUT_LOGGING
            // ReSharper disable InconsistentlySynchronizedField
            _registeredLoggers.Clear();
            _registeredLoggers = null;
            // ReSharper restore InconsistentlySynchronizedField
            _loggerFactory = null;
#endif
        }
    }

    #endregion
}

public class SimpleTestingContext : ITestingContext
{
    public SimpleTestingContext(IHostEnvironment host, IConfiguration config)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Config = config ?? throw new ArgumentNullException(nameof(config));
    }

    #region | ITestingContext implementation |

    public IHostEnvironment Host { get; }
    public IConfiguration Config { get; }

    #endregion
}

#if SIMPLE_HTTP_FACTORY

public class SimpleTestingHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
}

#endif

public class SimpleHostEnvironment : IHostEnvironment
{
    public SimpleHostEnvironment(string environmentName,
        IFileProvider contentRootFileProvider = null)
    {
        EnvironmentName = (string.IsNullOrWhiteSpace(environmentName))
            ? "Unknown"
            : environmentName.Trim();
        // ReSharper disable once AssignNullToNotNullAttribute
        ContentRootFileProvider = contentRootFileProvider;
    }

    #region | IHostEnvironment implementation |

    public string ApplicationName { get; set; } = typeof(SimpleTestFixture).Assembly.GetName().Name!;
    public IFileProvider ContentRootFileProvider { get; set; }
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public string EnvironmentName { get; set; }

    #endregion
}

#if SIMPLE_OUTPUT_LOGGING

public class SimpleOutputLoggerFactory : IOutputLoggerFactory
{
    public ILogger GetLogger(ITestOutputHelper output) => new SimpleOutputLogger(output);
    public ILogger<T> GetLogger<T>(ITestOutputHelper output) => new SimpleOutputLogger<T>(output);
}

#endif

#endregion

#if SIMPLE_OUTPUT_LOGGING

public class SimpleTestOutputHelper : ITestOutputHelper
{
    private readonly ITestOutputHelper _wrappedOutput;
    private readonly StringBuilder _output = new();

    public SimpleTestOutputHelper(ITestOutputHelper wrappedOutput = null)
    {
        _wrappedOutput = wrappedOutput;
    }

    public bool AlwaysWriteToConsole { get; set; }

    private void WriteText(string text, bool withEndOfLine = false)
    {
        if (text != null)
        {
            if (withEndOfLine) { _output.AppendLine(text); }
            else { _output.Append(text); }

            if (AlwaysWriteToConsole
                || (_wrappedOutput == null)
                || (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))) //Need to write test output to console on Linux
            {
                if (withEndOfLine) { Console.WriteLine(text); }
                else { Console.Write(text); }
            }
            else
            {
                try
                {
                    //Note: writing to ITestOutputHelper can fail if the test has already completed
                    if (withEndOfLine) { _wrappedOutput.WriteLine(text); }
                    else { _wrappedOutput.Write(text); }
                }
                catch (Exception)
                {
                    if (withEndOfLine) { Console.WriteLine(text); }
                    else { Console.Write(text); }
                }
            }
        }
    }

    #region | ITestOutputHelper implementation |

    /// <inheritdoc />
    public string Output => _output.ToString();

    /// <inheritdoc />
    public void Write(string message) => WriteText(message);

    /// <inheritdoc />
    public void Write(string format, params object[] args) =>
        WriteText(string.Format(format, args));

    /// <inheritdoc />
    public void WriteLine(string message) => WriteText(message, withEndOfLine: true);

    /// <inheritdoc />
    public void WriteLine(string format, params object[] args) =>
        WriteText(string.Format(format, args), withEndOfLine: true);

    #endregion
}

public class SimpleOutputLoggerScope<TState> : IDisposable
{
#pragma warning disable IDE0052
    // ReSharper disable once NotAccessedField.Local
    private readonly TState _state;
#pragma warning restore IDE0052
    public SimpleOutputLoggerScope(TState state) { _state = state; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing) { } //Nothing to dispose yet
    }
}

internal class SimpleOutputLogWriter
{
    private readonly ITestOutputHelper _output;

    //Seems like the Xunit ISimpleOutputHelper doesn't like to write to the
    //  console when running 'dotnet test xyz.csproj' - at least on Linux
    public void WriteLine(string message, Exception exception = null)
    {
        message = message?.Trim() ?? string.Empty;

        if (exception != null
            && (!string.IsNullOrWhiteSpace(exception.Message)))
        {
            var exMessage = exception.GetType().Name;
            if ((!message.Contains(exMessage, StringComparison.InvariantCultureIgnoreCase))
                && (!message.Contains(exception.Message.Trim(), StringComparison.InvariantCultureIgnoreCase)))
            {
                exMessage += $": {exception.Message.Trim()}";
                message += (message == string.Empty)
                    ? exMessage
                    : $" - {exMessage}";
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Console.WriteLine(message);
        }
        else
        {
            _output.WriteLine(message);
        }
    }

    private SimpleOutputLogWriter(ITestOutputHelper output) { _output = output; }
    internal static SimpleOutputLogWriter GetInstance(ITestOutputHelper output) => new(output);
}

public class SimpleOutputLogger : ILogger
{
    private readonly string _name;
    private readonly SimpleOutputLogWriter _writer;

    public SimpleOutputLogger(ITestOutputHelper output, string name = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        _writer = SimpleOutputLogWriter.GetInstance(output);
        _name = (string.IsNullOrWhiteSpace(name)) ? null : name.Trim();
    }

    #region | ILogger implementation |

    public IDisposable BeginScope<TState>(TState state) => new SimpleOutputLoggerScope<TState>(state);

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        var evtId = (eventId.Id > 0)
            ? $" - Event: {eventId.Id}"
            : string.Empty;

        var timestamp = DateTime.Now.ToString("s").Replace('T', ' ');

        _writer.WriteLine($"{timestamp} [{logLevel.ToString().ToUpperInvariant()}{evtId}]"
                          + $" {((_name == null) ? "" : $"{_name} - ")}{formatter(state, exception)}",
            exception);
    }

    #endregion
}

public class SimpleOutputLogger<T> : SimpleOutputLogger, ILogger<T>
{
    public SimpleOutputLogger(ITestOutputHelper output)
        : base(output, typeof(T).Name) { }
}

#endif
