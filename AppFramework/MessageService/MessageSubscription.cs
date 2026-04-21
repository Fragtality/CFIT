using CFIT.AppTools;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.AppFramework.MessageService
{
    public interface IMessageSubscription : IDisposable
    {
        public bool IsReceived { get; }
        public bool IsSubscribed { get; }
        public int Subscriptions { get; }

        public event Func<Task> OnMessage;

        public void AddSubscription(Func<Task> callback);
        public void RemoveSubscription(Func<Task> callback);
        public void Reset();
    }

    public class MessageSubscription<TMessage>(CancellationToken token) : IRecipient<TMessage>, IMessageSubscription where TMessage : AppMessage
    {
        protected virtual CancellationToken Token { get; } = token;
        public virtual bool IsReceived { get; protected set; } = false;
        public virtual bool IsSubscribed => Subscriptions > 0;
        public virtual int Subscriptions { get; protected set; } = 0;
        protected bool isDisposed = false;

        public event Func<Task> OnMessage;

        public virtual void Receive(TMessage message)
        {
            IsReceived = true;
            _ = TaskTools.RunPool(() => OnMessage?.Invoke(), Token);
        }

        public virtual void AddSubscription(Func<Task> callback)
        {
            Subscriptions++;
            OnMessage += callback;
        }

        public virtual void RemoveSubscription(Func<Task> callback)
        {
            Subscriptions--;
            OnMessage -= callback;
        }

        public virtual void Reset()
        {
            IsReceived = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    IsReceived = false;
                    OnMessage = null;
                    Subscriptions = 0;
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