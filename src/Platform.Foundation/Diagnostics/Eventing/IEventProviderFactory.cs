using System;

namespace CodeBrix.Platform.Diagnostics.Eventing; //Was previously: Uno.Diagnostics.Eventing

/// <summary>
/// Defines a factory of EventProviders
/// </summary>
public interface IEventProviderFactory
{
    /// <summary>
    /// Gets a event provider.
    /// </summary>
    /// <param name="provider">The ID of the event provider</param>
    /// <returns>An event provider</returns>
    IEventProvider GetProvider(Guid provider);
}