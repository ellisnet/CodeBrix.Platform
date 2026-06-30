using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeBrix.Platform.Simple;

public interface ISimpleServiceResolver : IServiceProvider
{
    T GetService<T>() where T : class;
    IEnumerable<T> GetServices<T>() where T : class;
}

public interface IHostBuilderProvider
{
    IHostBuilder CreateDefaultBuilder();
    IHostBuilder CreateDefaultBuilder(string[] args);
}

public class SimpleServiceResolver : ISimpleServiceResolver
{
    private readonly IHost _host;

    // ReSharper disable once InconsistentNaming
    private static SimpleServiceResolver _instance;
    public static SimpleServiceResolver Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new InvalidOperationException(
                    $"The {nameof(SimpleServiceResolver)}.{nameof(CreateInstance)}() static method must be called at application start.");
            }

            return _instance;
        }
    }

    public static void CreateInstance(IHostBuilderProvider host, Action<IServiceCollection> configureServices, string[] args = null)
    {
        _instance = new SimpleServiceResolver(host, configureServices, args);
    }

    public static void CreateInstance(IHost host)
    {
        _instance = new SimpleServiceResolver(host);
    }

    private SimpleServiceResolver(IHostBuilderProvider host, Action<IServiceCollection> configureServices, string[] args = null)
    {
        ArgumentNullException.ThrowIfNull(host);

        var builder = (args == null)
            ? host.CreateDefaultBuilder()
            : host.CreateDefaultBuilder(args);

        _host = builder
            .ConfigureServices((context, services) =>
            {
                configureServices.Invoke(services);

                if (!services.IsRegistered<ISimpleServiceResolver>())
                {
                    services.AddSingleton<ISimpleServiceResolver>((svc) => this);
                }

                services.AutoRegisterServices([Assembly.GetExecutingAssembly()]);
                services.AddSimpleMessaging();
            })
            .Build();
    }

    private SimpleServiceResolver(IHost host)
    {
        _host = host;
    }

    public async Task StartupHost() => await _host.StartAsync();

    public async Task ShutdownHost()
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    #region | ISimpleServiceResolver implementation |

    /// <inheritdoc />
    public object GetService(Type serviceType) => _host.Services.GetService(serviceType);

    /// <inheritdoc />
    public T GetService<T>() where T : class => _host.Services.GetRequiredService<T>();

    /// <inheritdoc />
    public IEnumerable<T> GetServices<T>() where T : class => _host.Services.GetServices<T>();

    #endregion
}

public interface IAutoRegisterServices
{
    void RegisterServices(IServiceCollection services);
}

public static class SimpleServiceExtensions
{
    public static bool IsRegistered(this IServiceCollection services, Type serviceType) =>
        (services != null && serviceType != null)
        && services.Any(a => serviceType.IsAssignableFrom(a.ServiceType));

    public static bool IsRegistered<TService>(this IServiceCollection services) =>
        IsRegistered(services, typeof(TService));

    public static IServiceCollection AutoRegisterServices(this IServiceCollection services, IList<Assembly> fromAssemblies)
    {
        if (services != null && fromAssemblies != null)
        {
            foreach (var assembly in fromAssemblies.Distinct())
            {
                foreach (var registerType in assembly.GetTypes()
                             .Where(w => w.IsAssignableTo(typeof(IAutoRegisterServices))))
                {
                    //needs an empty constructor
                    if (registerType.GetConstructor(Type.EmptyTypes) != null)
                    {
                        try
                        {
                            ((IAutoRegisterServices)Activator.CreateInstance(registerType))!.RegisterServices(services);
                        }
                        catch (Exception e)
                        {
                            throw new TypeLoadException(
                                $"Error while calling {nameof(IAutoRegisterServices.RegisterServices)}()"
                                + $" on type: {registerType.Namespace}.{registerType.Name}"
                                , e);
                        }
                    }
                }
            }
        }

        return services;
    }

    public static IServiceCollection AutoRegisterServices(this IServiceCollection services, IList<Type> fromAssembliesContainingTypes) =>
        AutoRegisterServices(services, fromAssembliesContainingTypes?.Select(s => s.Assembly).ToList());

    public static IServiceCollection AddSimpleMessaging(this IServiceCollection services)
    {
        SimpleMessaging.ConfigureServices(services);
        return services;
    }
}
