#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// Maps an owner type (a visual, brush, clip or shape type) to the factory of its Skia platform state. Factories are
/// registered per type by the assembly's <see cref="SkiaPlatformBootstrap"/>; an owner whose own type has no
/// factory gets the factory of its nearest registered base type (so a Skia-only subclass defined elsewhere, for
/// example an SKCanvas visual deriving from <c>ContainerVisual</c>, gets the container's). The resolution is done once
/// per concrete type and cached, so creating platform state is a lock-free dictionary read plus one constructor call.
/// </summary>
/// <typeparam name="TOwner">The owner base type.</typeparam>
/// <typeparam name="TPlatform">The platform state base type.</typeparam>
internal sealed class SkiaPlatformFactoryRegistry<TOwner, TPlatform>
	where TOwner : class
	where TPlatform : class
{
	private readonly Dictionary<Type, Func<TOwner, TPlatform>> _registered = new();
	private readonly ConcurrentDictionary<Type, Func<TOwner, TPlatform>> _resolved = new();
	private readonly Func<Type, Func<TOwner, TPlatform>> _resolve;

	/// <summary>
	/// Creates an empty registry.
	/// </summary>
	internal SkiaPlatformFactoryRegistry()
	{
		_resolve = Resolve;
	}

	/// <summary>
	/// Registers the factory of the platform state of owners of type <typeparamref name="T"/> (and of its subtypes
	/// that have no factory of their own). Called by the bootstrap only, before any owner is created.
	/// </summary>
	/// <typeparam name="T">The owner type.</typeparam>
	/// <param name="factory">Creates the platform state of an owner.</param>
	internal void Register<T>(Func<T, TPlatform> factory)
		where T : TOwner
	{
		_registered[typeof(T)] = owner => factory((T)owner);
	}

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The owner.</param>
	/// <returns>The platform state.</returns>
	internal TPlatform Create(TOwner owner)
	{
		var type = owner.GetType();
		if (!_resolved.TryGetValue(type, out var factory))
		{
			factory = _resolved.GetOrAdd(type, _resolve);
		}

		return factory(owner);
	}

	private Func<TOwner, TPlatform> Resolve(Type type)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			if (_registered.TryGetValue(current, out var factory))
			{
				return factory;
			}
		}

		throw new InvalidOperationException($"No Skia platform factory is registered for {type.FullName} or any of its base types.");
	}
}
