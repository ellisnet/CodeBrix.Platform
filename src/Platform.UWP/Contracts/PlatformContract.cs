using System;
using CodeBrix.Platform.Foundation.Extensibility;

namespace CodeBrix.Platform.Contracts;

/// <summary>
/// Resolves a platform contract of this assembly from the <see cref="ApiExtensibility"/> registry.
/// </summary>
/// <remarks>
/// Callers resolve a service contract once and keep it in a static field; this is never called per operation.
/// </remarks>
internal static class PlatformContract
{
	/// <summary>
	/// Returns the registered implementation of the platform contract <typeparamref name="TContract"/>.
	/// </summary>
	/// <typeparam name="TContract">The contract interface.</typeparam>
	/// <returns>The implementation registered by the platform bootstrap.</returns>
	/// <exception cref="InvalidOperationException">No implementation of <typeparamref name="TContract"/> is registered.</exception>
	internal static TContract Resolve<TContract>()
		where TContract : class
	{
		if (ApiExtensibility.CreateInstance<TContract>(typeof(TContract), out var implementation))
		{
			return implementation;
		}

		throw new InvalidOperationException(
			$"The platform contract {typeof(TContract).FullName} is not registered. "
			+ "The platform bootstrap must run before it is used.");
	}

	/// <summary>
	/// Returns the registered implementation of the OPTIONAL platform contract <typeparamref name="TContract"/>, or
	/// <see langword="null"/> when the platform does not implement it (the caller then keeps its built-in behavior).
	/// </summary>
	/// <typeparam name="TContract">The contract interface.</typeparam>
	/// <returns>The implementation registered by the platform bootstrap, or <see langword="null"/>.</returns>
	internal static TContract TryResolve<TContract>()
		where TContract : class
		=> ApiExtensibility.CreateInstance<TContract>(typeof(TContract), out var implementation) ? implementation : null;
}
