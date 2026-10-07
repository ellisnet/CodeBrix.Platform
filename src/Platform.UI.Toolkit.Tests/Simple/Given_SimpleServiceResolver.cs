using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Simple;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Simple;

[TestClass]
public class Given_SimpleServiceResolver
{
	public interface IGreetingService
	{
		string Greet();
	}

	private sealed class GreetingService : IGreetingService
	{
		public string Greet() => "hello";
	}

	/// <summary>
	/// Discovered by <see cref="SimpleServiceExtensions.AutoRegisterServices(IServiceCollection, System.Collections.Generic.IList{Assembly})"/>
	/// when it scans this test assembly.
	/// </summary>
	public sealed class AutoRegisteredServices : IAutoRegisterServices
	{
		public void RegisterServices(IServiceCollection services) =>
			services.AddSingleton<IGreetingService, GreetingService>();
	}

	private sealed class FakeHost(IServiceProvider services) : IHost
	{
		public IServiceProvider Services => services;
		public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
		public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
		public void Dispose() { }
	}

	//SimpleServiceResolver.Instance is app-global static state; reset it around every test
	//so this class neither depends on nor pollutes the rest of the suite.
	private static void ResetInstance() =>
		typeof(SimpleServiceResolver)
			.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!
			.SetValue(null, null);

	[TestInitialize]
	public void Init() => ResetInstance();

	[TestCleanup]
	public void Cleanup() => ResetInstance();

	[TestMethod]
	public void When_Instance_Not_Created_Throws() =>
		Assert.ThrowsExactly<InvalidOperationException>(() => _ = SimpleServiceResolver.Instance);

	[TestMethod]
	public void When_CreateInstance_From_Host_Resolves_Services()
	{
		//Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IGreetingService, GreetingService>();
		SimpleServiceResolver.CreateInstance(new FakeHost(services.BuildServiceProvider()));

		//Act + Assert
		SimpleServiceResolver.Instance.GetService<IGreetingService>().Greet().Should().Be("hello");
		Assert.IsNotNull(SimpleServiceResolver.Instance.GetService(typeof(IGreetingService)));
	}

	[TestMethod]
	public void When_GetServices_Returns_All_Registrations()
	{
		//Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IGreetingService, GreetingService>();
		services.AddSingleton<IGreetingService, GreetingService>();
		SimpleServiceResolver.CreateInstance(new FakeHost(services.BuildServiceProvider()));

		//Act
		var resolved = SimpleServiceResolver.Instance.GetServices<IGreetingService>();

		//Assert
		System.Linq.Enumerable.Count(resolved).Should().Be(2);
	}

	[TestMethod]
	public void When_IsRegistered_Reports_Registration_State()
	{
		//Arrange
		var services = new ServiceCollection();

		//Act + Assert
		services.IsRegistered<IGreetingService>().Should().Be(false);
		services.AddSingleton<IGreetingService, GreetingService>();
		services.IsRegistered<IGreetingService>().Should().Be(true);
	}

	[TestMethod]
	public void When_AutoRegisterServices_Scans_Assembly()
	{
		//Arrange
		var services = new ServiceCollection();

		//Act
		services.AutoRegisterServices([typeof(Given_SimpleServiceResolver).Assembly]);

		//Assert
		services.IsRegistered<IGreetingService>().Should().Be(true);
	}

	[TestMethod]
	public void When_AutoRegisterServices_By_Containing_Type()
	{
		//Arrange
		var services = new ServiceCollection();

		//Act
		services.AutoRegisterServices([typeof(Given_SimpleServiceResolver)]);

		//Assert
		services.IsRegistered<IGreetingService>().Should().Be(true);
	}

	[TestMethod]
	public void When_AddSimpleMessaging_Registers_The_Shared_Instance()
	{
		//Arrange
		var services = new ServiceCollection();

		//Act
		services.AddSimpleMessaging();

		//Assert
		var provider = services.BuildServiceProvider();
		ReferenceEquals(provider.GetRequiredService<ISimpleMessaging>(), SimpleMessaging.Instance)
			.Should().Be(true);
	}

	/// <summary>Exposes the view model's protected service lookups to the tests below.</summary>
	private sealed class ServiceLookupViewModel : SimpleViewModel
	{
		public static T Lookup<T>() where T : class => GetService<T>();

		public static T Lookup<T>(bool returnNullForUnregistered) where T : class =>
			GetService<T>(returnNullForUnregistered: returnNullForUnregistered);
	}

	[TestMethod]
	public void When_ViewModel_GetService_Unregistered_Throws()
	{
		//Arrange
		SimpleServiceResolver.CreateInstance(new FakeHost(new ServiceCollection().BuildServiceProvider()));

		//Act + Assert
		Assert.ThrowsExactly<InvalidOperationException>(() => ServiceLookupViewModel.Lookup<IGreetingService>());
	}

	[TestMethod]
	public void When_ViewModel_GetService_ReturnNullForUnregistered_Returns_Null()
	{
		//Arrange
		SimpleServiceResolver.CreateInstance(new FakeHost(new ServiceCollection().BuildServiceProvider()));

		//Act
		var resolved = ServiceLookupViewModel.Lookup<IGreetingService>(returnNullForUnregistered: true);

		//Assert
		Assert.IsNull(resolved);
	}

	[TestMethod]
	public void When_ViewModel_GetService_ReturnNullForUnregistered_Still_Resolves_Registered()
	{
		//Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IGreetingService, GreetingService>();
		SimpleServiceResolver.CreateInstance(new FakeHost(services.BuildServiceProvider()));

		//Act
		var resolved = ServiceLookupViewModel.Lookup<IGreetingService>(returnNullForUnregistered: true);

		//Assert
		resolved.Greet().Should().Be("hello");
	}

	[TestMethod]
	public void When_ViewModel_GetService_ReturnNullForUnregistered_False_Throws()
	{
		//Arrange
		SimpleServiceResolver.CreateInstance(new FakeHost(new ServiceCollection().BuildServiceProvider()));

		//Act + Assert
		Assert.ThrowsExactly<InvalidOperationException>(
			() => ServiceLookupViewModel.Lookup<IGreetingService>(returnNullForUnregistered: false));
	}
}
