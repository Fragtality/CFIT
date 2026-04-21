using CFIT.AppFramework.AppConfig;
using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.AppFramework.MessageService
{
    public class AppMessage() : ValueChangedMessage<object>(new object()) { }

    public class AppMessageService(CancellationToken token) : IDisposable
    {
        public virtual CancellationToken Token { get; } = token;
        public virtual StrongReferenceMessenger Messenger { get; } = new();
        public virtual ConcurrentDictionary<Type, IMessageSubscription> Subscriptions { get; } = [];
        protected bool isDisposed = false;

        public virtual IMessageSubscription Subscribe<TMessage>(Func<Task> callback) where TMessage : AppMessage
        {
            if (Subscriptions.TryGetValue(typeof(TMessage), out var subscription))
            {
                subscription.AddSubscription(callback);
                return subscription;
            }
            else
            {
                MessageSubscription<TMessage> newSub = new(Token);
                Messenger.Register<TMessage>(newSub);
                newSub.AddSubscription(callback);
                Subscriptions.Add(typeof(TMessage), newSub);
                return newSub;
            }
        }

        public virtual void Unsubscribe<TMessage>(Func<Task> callback) where TMessage : AppMessage
        {
            if (Subscriptions.TryGetValue(typeof(TMessage), out var subscription))
            {
                subscription.RemoveSubscription(callback);
                if (!subscription.IsSubscribed)
                {
                    Messenger.Unregister<TMessage>(subscription);
                    subscription.Dispose();
                    Subscriptions.Remove(typeof(TMessage));
                }
            }
        }

        public async Task WaitReceived<TMessage>(CancellationToken? token = null, int interval = 0, bool reset = false, int timeoutMs = int.MaxValue) where TMessage : AppMessage
        {
#pragma warning disable
            var func = () => Task.CompletedTask;
#pragma warning enable
            var subscription = Subscribe<TMessage>(func);

            CancellationToken cancellationToken = token ?? Token;
            if (reset)
                subscription.Reset();

            if (interval <= 0)
                interval = ProductDefinitionBase.MessageCheckIntervalMs;
            int waitTime = 0;

            try
            {
                while (!subscription.IsReceived && !cancellationToken.IsCancellationRequested && waitTime < timeoutMs)
                {
                    await Task.Delay(interval, cancellationToken);
                    waitTime += interval;
                }

                if (waitTime >= timeoutMs)
                    Logger.Debug($"Receiver timed out");
            }
            catch { }

            Unsubscribe<TMessage>(func);
        }

        public virtual TMessage Send<TMessage>() where TMessage : AppMessage
        {
            return Messenger.Send<TMessage>(typeof(TMessage).CreateInstance<TMessage>());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    foreach (var sub in Subscriptions.Values)
                        sub.Dispose();
                    Subscriptions.Clear();
                    Messenger.Reset();
                }
                isDisposed = true;
            }
        }

        public virtual void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}