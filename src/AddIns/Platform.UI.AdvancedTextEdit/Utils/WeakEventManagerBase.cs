#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

//was previously: ICSharpCode.AvalonEdit/Utils/WeakEventManagerBase.cs in the AvalonEdit repo
//(MIT). Upstream derives from WPF's System.Windows.WeakEventManager, which supplies the
//per-source listener table, DeliverEvent and the CurrentManager registry; this framework has no
//such base, so those pieces are implemented here directly. The public surface consumers see -
//AddListener/RemoveListener, StartListening/StopListening(TEventSource), DeliverEvent - is
//unchanged, so the editor's weak-event manager subclasses port mechanically.

/// <summary>
/// Base class for the weak event manager pattern: listeners subscribe to a source's event
/// through a per-event manager singleton, and neither the source nor the manager keeps the
/// listener alive.
/// </summary>
/// <typeparam name="TManager">The concrete manager type (self-referential).</typeparam>
/// <typeparam name="TEventSource">The type of the event source.</typeparam>
/// <remarks>
/// A subclass implements <see cref="StartListening"/> and <see cref="StopListening"/> by
/// attaching or detaching <see cref="DeliverEvent"/> to one specific event of the source.
/// Listener registrations hold the source weakly (a dead source's registrations vanish with it)
/// and hold each listener weakly (a dead listener is pruned on the next delivery).
/// </remarks>
public abstract class WeakEventManagerBase<TManager, TEventSource>
	where TManager : WeakEventManagerBase<TManager, TEventSource>, new()
	where TEventSource : class
{
	private static TManager? _currentManager;
	private static readonly object _lock = new();

	// The table holds sources weakly: when a source is collected, its listener list goes with
	// it, which is exactly the lifetime the pattern wants. The manager's event-handler
	// subscription (source.Event += DeliverEvent) roots the manager from the source, never the
	// reverse.
	private readonly ConditionalWeakTable<TEventSource, List<WeakReference<IWeakEventListener>>> _listeners = new();

	/// <summary>
	/// Creates the manager. Only the singleton created through <see cref="CurrentManager"/>
	/// should exist.
	/// </summary>
	protected WeakEventManagerBase()
	{
		Debug.Assert(GetType() == typeof(TManager));
	}

	/// <summary>
	/// Adds a weak event listener for a source.
	/// </summary>
	/// <param name="source">The event source to listen to.</param>
	/// <param name="listener">The listener to deliver events to.</param>
	public static void AddListener(TEventSource source, IWeakEventListener listener)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}

		if (listener is null)
		{
			throw new ArgumentNullException(nameof(listener));
		}

		CurrentManager.ProtectedAddListener(source, listener);
	}

	/// <summary>
	/// Removes a weak event listener from a source.
	/// </summary>
	/// <param name="source">The event source.</param>
	/// <param name="listener">The listener to remove.</param>
	public static void RemoveListener(TEventSource source, IWeakEventListener listener)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}

		if (listener is null)
		{
			throw new ArgumentNullException(nameof(listener));
		}

		CurrentManager.ProtectedRemoveListener(source, listener);
	}

	/// <summary>
	/// Gets the singleton manager instance for <typeparamref name="TManager"/>.
	/// </summary>
	protected static TManager CurrentManager
	{
		get
		{
			lock (_lock)
			{
				return _currentManager ??= new TManager();
			}
		}
	}

	/// <summary>
	/// Attaches the manager's event handler to the source. Called when the source gets its
	/// first listener.
	/// </summary>
	/// <param name="source">The event source.</param>
	protected abstract void StartListening(TEventSource source);

	/// <summary>
	/// Detaches the manager's event handler from the source. Called when the source's last
	/// listener is removed.
	/// </summary>
	/// <param name="source">The event source.</param>
	protected abstract void StopListening(TEventSource source);

	/// <summary>
	/// Delivers an event raised by a source to every live listener registered for it. Attach
	/// this method to the source's event in <see cref="StartListening"/>.
	/// </summary>
	/// <param name="sender">The event source that raised the event.</param>
	/// <param name="e">The event data.</param>
	/// <exception cref="InvalidOperationException">
	/// A live listener returned false from <see cref="IWeakEventListener.ReceiveWeakEvent"/>,
	/// which means it does not recognize this manager type - a programming error, matching the
	/// behavior of the pattern this replaces.
	/// </exception>
	protected void DeliverEvent(object? sender, EventArgs e)
	{
		if (sender is not TEventSource source)
		{
			return;
		}

		List<WeakReference<IWeakEventListener>>? list;
		lock (_lock)
		{
			if (!_listeners.TryGetValue(source, out list))
			{
				return;
			}
		}

		IWeakEventListener[] live;
		lock (list)
		{
			live = new IWeakEventListener[list.Count];
			var liveCount = 0;
			for (var i = list.Count - 1; i >= 0; i--)
			{
				if (list[i].TryGetTarget(out var listener))
				{
					live[liveCount++] = listener;
				}
				else
				{
					list.RemoveAt(i);
				}
			}

			// Collected in reverse while pruning; restore registration order for delivery.
			Array.Reverse(live, 0, liveCount);
			if (liveCount != live.Length)
			{
				Array.Resize(ref live, liveCount);
			}
		}

		foreach (var listener in live)
		{
			if (!listener.ReceiveWeakEvent(GetType(), sender, e))
			{
				throw new InvalidOperationException(
					$"The listener {listener.GetType().FullName} does not handle events from {GetType().FullName}.");
			}
		}
	}

	private void ProtectedAddListener(TEventSource source, IWeakEventListener listener)
	{
		List<WeakReference<IWeakEventListener>>? list;
		var start = false;
		lock (_lock)
		{
			if (!_listeners.TryGetValue(source, out list))
			{
				list = [];
				_listeners.Add(source, list);
				start = true;
			}
		}

		lock (list)
		{
			list.Add(new WeakReference<IWeakEventListener>(listener));
		}

		if (start)
		{
			StartListening(source);
		}
	}

	private void ProtectedRemoveListener(TEventSource source, IWeakEventListener listener)
	{
		List<WeakReference<IWeakEventListener>>? list;
		lock (_lock)
		{
			if (!_listeners.TryGetValue(source, out list))
			{
				return;
			}
		}

		var stop = false;
		lock (list)
		{
			for (var i = list.Count - 1; i >= 0; i--)
			{
				if (!list[i].TryGetTarget(out var existing))
				{
					list.RemoveAt(i);
				}
				else if (ReferenceEquals(existing, listener))
				{
					list.RemoveAt(i);
					// Remove only the first (newest) registration, matching add/remove pairing.
					break;
				}
			}

			if (list.Count == 0)
			{
				stop = true;
			}
		}

		if (stop)
		{
			lock (_lock)
			{
				_listeners.Remove(source);
			}

			StopListening(source);
		}
	}
}
