using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CodeBrix.Platform.Simple;

public interface ISimpleMessaging
{
    void Send<TSender, TArgs>(TSender sender, string message, TArgs args)
        where TSender : class;

    void Send<TSender>(TSender sender, string message)
        where TSender : class;

    void Subscribe<TSender, TArgs>(object subscriber, string message, Action<TSender, TArgs> callback, TSender source)
        where TSender : class;

    void SubscribeFrom<TSender>(object subscriber, string message, Action<TSender> callback, TSender source)
        where TSender : class;

    void Subscribe<TArgs>(object subscriber, string message, Action<TArgs> callback);

    void Subscribe<TSender, TArgs>(object subscriber, string message, Func<TSender, TArgs, Task> callback, TSender source)
        where TSender : class;

    void SubscribeFrom<TSender>(object subscriber, string message, Func<TSender, Task> callback, TSender source)
        where TSender : class;

    void Subscribe<TArgs>(object subscriber, string message, Func<TArgs, Task> callback);

    void Unsubscribe<TSender, TArgs>(object subscriber, string message)
        where TSender : class;

    void UnsubscribeFrom<TSender>(object subscriber, string message)
        where TSender : class;

    void Unsubscribe<TArgs>(object subscriber, string message);
}

public class SimpleMessaging : ISimpleMessaging
{
    public static ISimpleMessaging Instance { get; } = new SimpleMessaging();

    public static void ConfigureServices(IServiceCollection services)
    {
        if (services != null && (!services.IsRegistered<ISimpleMessaging>()))
        {
            services.AddSingleton(Instance);
        }
    }

    private class Sender : Tuple<string, Type, Type>
    {
        public Sender(string message, Type senderType, Type argType)
            : base(message, senderType, argType) { }
    }

    private delegate bool Filter(object sender);

    private class MaybeWeakReference
    {
        WeakReference DelegateWeakReference { get; }
        object DelegateStrongReference { get; }

        readonly bool _isStrongReference;

        public MaybeWeakReference(object subscriber, object delegateSource)
        {
            if (subscriber.Equals(delegateSource))
            {
                // The target is the subscriber; we can use a weak reference
                DelegateWeakReference = new WeakReference(delegateSource);
                _isStrongReference = false;
            }
            else
            {
                DelegateStrongReference = delegateSource;
                _isStrongReference = true;
            }
        }

        public object Target => _isStrongReference ? DelegateStrongReference : DelegateWeakReference?.Target;
        public bool IsAlive => _isStrongReference
                               || (DelegateWeakReference?.IsAlive ?? false);
    }

    private class Subscription : IDisposable
    {
        public Subscription(
            object subscriber,
            object delegateSource,
            MethodInfo syncMethod,
            Func<object, object, Task> asyncMethod,
            Filter filter)
        {
            Subscriber = new WeakReference(subscriber);
            DelegateSource = new MaybeWeakReference(subscriber, delegateSource);
            SyncMethod = syncMethod;
            AsyncMethod = asyncMethod;
            Filter = filter;
        }

        public WeakReference Subscriber { get; }
        private MaybeWeakReference DelegateSource { get; }
        private MethodInfo SyncMethod { get; set; }
        private Func<object, object, Task> AsyncMethod { get; set; }
        // ReSharper disable once MemberHidesStaticFromOuterClass
        private Filter Filter { get; }
        private SemaphoreSlim _asyncLocker = new(1, 1);
        private bool _isDisposed;

        public void InvokeCallback(object sender, object args)
        {
            if (_isDisposed) { return; }

            if (sender != null && (!Filter(sender))) { return; }

            if (AsyncMethod != null)
            {
                //Because of the nature of Subscription Callbacks, we must always invoke async subscription callback functions
                //  as fire-and-forget - there is no way to actually await them - and any UI thread-affecting code in the
                //  callback will always need to be run as "InvokeOnMainThread".
                //  They will NOT be invoked in a thread-safe way - thread-safety must be ensured by the 

                // ReSharper disable once AsyncVoidLambda
                new Task(async () =>
                {
                    try
                    {
                        if (!_isDisposed)
                        {
                            await _asyncLocker.WaitAsync();
                        }

                        if (!_isDisposed)
                        {
                            await AsyncMethod.Invoke(sender, args);
                        }
                    }
                    finally
                    {
                        if (!_isDisposed)
                        {
                            _asyncLocker.Release();
                        }
                    }

                }).Start();
            }
            else if (SyncMethod != null)
            {
                if (SyncMethod.IsStatic)
                {
                    SyncMethod.Invoke(null,
                        (SyncMethod.GetParameters().Length == 1)
                            ? [sender ?? args]
                            : [sender, args]);
                    return;
                }

                var target = DelegateSource.Target;

                if (target == null) { return; }

                SyncMethod.Invoke(target,
                    (SyncMethod.GetParameters().Length == 1)
                        ? [sender ?? args]
                        : [sender, args]);
            }
        }

        public bool CanBeRemoved()
        {
            return (!Subscriber.IsAlive) || (!DelegateSource.IsAlive);
        }

        #region | IDisposable implementation |

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                SyncMethod = null;
                AsyncMethod = null;
                _asyncLocker?.Dispose();
                _asyncLocker = null;
            }
        }

        #endregion
    }

    private readonly Dictionary<Sender, List<Subscription>> _subscriptions = [];
    private readonly Lock _subscriptionLocker = new();

    private void InnerSend(
        string message,
        Type senderType,
        Type argType,
        object sender,
        object args)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        //Item1 = the subscription
        //Item2 = is it explicit?
        var matchingSubscriptions = new List<Tuple<Subscription, bool>>();
        var typedSubscriptions = new List<Subscription>();
        var genericSubscriptions = new List<Subscription>();

        lock (_subscriptionLocker)
        {
            // Step 1 - look for subscriptions that explicitly reference this senderType
            var key = new Sender(message, senderType, argType);
            if (_subscriptions.TryGetValue(key, out var typedSubs))
            {
                typedSubscriptions.AddRange(typedSubs);
                matchingSubscriptions.AddRange(typedSubscriptions.Select(s => Tuple.Create(s, true)));
            }

            //Step 2 - look for subscriptions that reference the generic 'object' senderType
            key = new Sender(message, typeof(object), argType);
            if (_subscriptions.TryGetValue(key, out var genericSubs))
            {
                genericSubscriptions.AddRange(genericSubs);
                matchingSubscriptions.AddRange(genericSubscriptions.Select(s => Tuple.Create(s, false)));
            }
        }

        foreach (var subscription in matchingSubscriptions)
        {
            if (subscription.Item1.Subscriber.Target != null
                && (typedSubscriptions.Contains(subscription.Item1) || genericSubscriptions.Contains(subscription.Item1)))
            {
                //If the senderType was explicitly referenced, send the 'sender' - otherwise send null
                subscription.Item1.InvokeCallback((subscription.Item2 ? sender : null), args);
            }
        }
    }

    private void InnerSubscribe(
        object subscriber,
        string message,
        Type senderType,
        Type argType,
        object target,
        MethodInfo syncMethod,
        Func<object, object, Task> asyncMethod,
        Filter filter)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var key = new Sender(message, senderType, argType);
        var value = new Subscription(subscriber, target, syncMethod, asyncMethod, filter);

        lock (_subscriptionLocker)
        {
            if (_subscriptions.TryGetValue(key, out var subs))
            {
                subs.Add(value);
            }
            else
            {
                _subscriptions.Add(key, [value]);
            }
        }
    }

    private void InnerUnsubscribe(
        string message,
        Type senderType,
        Type argType,
        object subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber, nameof(subscriber));
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var key = new Sender(message, senderType, argType);
        lock (_subscriptionLocker)
        {
            if (_subscriptions.TryGetValue(key, out var subs))
            {
                var toRemove = subs.Where(w => w.CanBeRemoved()
                                               || w.Subscriber.Target == subscriber).ToArray();
                Array.ForEach(toRemove, f =>
                {
                    f?.Dispose();
                    subs.Remove(f);
                });
                if (subs.Count < 1) { _subscriptions.Remove(key); }
            }
        }
    }

    #region | ISimpleMessaging implementation |

    void ISimpleMessaging.Send<TSender, TArgs>(TSender sender, string message, TArgs args)
    {
        ArgumentNullException.ThrowIfNull(sender, nameof(sender));
        InnerSend(message, typeof(TSender), typeof(TArgs), sender, args);
    }

    void ISimpleMessaging.Send<TSender>(TSender sender, string message)
    {
        ArgumentNullException.ThrowIfNull(sender, nameof(sender));
        InnerSend(message, typeof(TSender), null, sender, null);
    }

    void ISimpleMessaging.Subscribe<TSender, TArgs>(
        object subscriber,
        string message,
        Action<TSender, TArgs> callback,
        TSender source)
        where TSender : class
    {
        ArgumentNullException.ThrowIfNull(subscriber, nameof(subscriber));
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        InnerSubscribe(subscriber, message, typeof(TSender), typeof(TArgs), callback.Target, callback.GetMethodInfo(), null,
            filter: (sender) =>
            {
                var send = (TSender)sender;
                return (source == null || send == source);
            });
    }

    void ISimpleMessaging.SubscribeFrom<TSender>(
        object subscriber,
        string message,
        Action<TSender> callback,
        TSender source)
        where TSender : class
    {
        ArgumentNullException.ThrowIfNull(subscriber, nameof(subscriber));
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        InnerSubscribe(subscriber, message, typeof(TSender), null, callback.Target, callback.GetMethodInfo(), null,
            filter: (sender) =>
            {
                var send = (TSender)sender;
                return (source == null || send == source);
            });
    }

    void ISimpleMessaging.Subscribe<TArgs>(object subscriber, string message, Action<TArgs> callback)
    {
        ArgumentNullException.ThrowIfNull(subscriber, nameof(subscriber));
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        InnerSubscribe(subscriber, message, typeof(object), typeof(TArgs), callback.Target, callback.GetMethodInfo(), null,
            filter: _ => true); //filter won't be used, for 'generic' object subscriptions
    }

    void ISimpleMessaging.Subscribe<TSender, TArgs>(
        object subscriber,
        string message,
        Func<TSender, TArgs, Task> callback,
        TSender source)
        where TSender : class
    {
        ArgumentNullException.ThrowIfNull(subscriber, nameof(subscriber));
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        Task AsyncMethod(object sender, object args)
        {
            if (sender != null && sender.GetType().IsAssignableTo(typeof(TSender)))
            {
                var typedArgs = (args != null && args.GetType().IsAssignableTo(typeof(TArgs)))
                    ? (TArgs)args
                    : default;
                return callback.Invoke((TSender)sender, typedArgs);
            }

            return Task.Run(() => { });
        }

        InnerSubscribe(subscriber, message, typeof(TSender), typeof(TArgs), callback.Target, null, AsyncMethod,
            filter: (sender) =>
            {
                var send = (TSender)sender;
                return (source == null || send == source);
            });
    }

    void ISimpleMessaging.SubscribeFrom<TSender>(
        object subscriber,
        string message,
        Func<TSender, Task> callback,
        TSender source)
        where TSender : class
    {
        ArgumentNullException.ThrowIfNull(subscriber, nameof(subscriber));
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        Task AsyncMethod(object sender, object args)
        {
            var typedSender = (sender != null && sender.GetType().IsAssignableTo(typeof(TSender)))
                ? (TSender)sender
                : (args != null && args.GetType().IsAssignableTo(typeof(TSender)))
                    ? (TSender)args
                    : null;
            return (typedSender != null)
                ? callback.Invoke(typedSender)
                : Task.Run(() => { });
        }

        InnerSubscribe(subscriber, message, typeof(TSender), null, callback.Target, null, AsyncMethod,
            filter: (sender) =>
            {
                var send = (TSender)sender;
                return (source == null || send == source);
            });
    }

    void ISimpleMessaging.Subscribe<TArgs>(object subscriber, string message, Func<TArgs, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(subscriber, nameof(subscriber));
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        Task AsyncMethod(object sender, object args)
        {
            if (args != null && args.GetType().IsAssignableTo(typeof(TArgs)))
            {
                return callback.Invoke((TArgs)args);
            }

            if (sender != null && sender.GetType().IsAssignableTo(typeof(TArgs)))
            {
                return callback.Invoke((TArgs)sender);
            }

            return Task.Run(() => { });
        }

        InnerSubscribe(subscriber, message, typeof(object), typeof(TArgs), callback.Target, null, AsyncMethod,
            filter: _ => true); //filter won't be used, for 'generic' object subscriptions
    }

    void ISimpleMessaging.Unsubscribe<TSender, TArgs>(object subscriber, string message) =>
        InnerUnsubscribe(message, typeof(TSender), typeof(TArgs), subscriber);

    void ISimpleMessaging.UnsubscribeFrom<TSender>(object subscriber, string message) =>
        InnerUnsubscribe(message, typeof(TSender), null, subscriber);

    void ISimpleMessaging.Unsubscribe<TArgs>(object subscriber, string message) =>
        InnerUnsubscribe(message, typeof(object), typeof(TArgs), subscriber);

    #endregion
}
