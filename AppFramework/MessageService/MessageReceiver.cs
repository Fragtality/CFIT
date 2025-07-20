using CFIT.AppFramework.AppConfig;
using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.AppFramework.MessageService
{
    public interface IMessageReceiver
    {
        public bool IsReceived { get; }
        public bool IsCanceled { get; }
        public void Clear();
        public void Cancel();
        public void Dispose();
    }

    public class MessageReceiver<TMessage> : IRecipient<TMessage>, IMessageReceiver, IDisposable where TMessage : AppMessage
    {
        public event Action<TMessage> OnMessage;

        protected virtual CancellationToken Token { get { return MessageService.Token; } }
        protected virtual AppMessageService MessageService { get; }
        protected virtual int CheckIntervalMs { get; }
        protected virtual ConcurrentQueue<TMessage> ReceivedMessages { get; } = [];
        public virtual bool IsReceived { get { return !ReceivedMessages.IsEmpty; } }
        public virtual bool IsCanceled { get; protected set; } = false;
        protected bool isDisposed = false;

        public MessageReceiver(AppMessageService service, int checkIntervalMs = ProductDefinitionBase.MessageCheckIntervalMs)
        {
            MessageService = service;
            CheckIntervalMs = checkIntervalMs;
            MessageService.Messenger.Register<TMessage>(this);
        }

        public void Receive(TMessage message)
        {
            ReceivedMessages.Enqueue(message);
            TaskTools.RunLogged(() => {
                OnMessage?.Invoke(message);
            }, Token);
        }

        public async Task<TMessage> ReceiveAsync(bool clearQueue = false, int timeoutMs = int.MaxValue, CancellationToken? token = null)
        {
            CancellationToken cancellationToken = token ?? Token;
            if (clearQueue)
                Clear();

            int waitTime = 0;
            while (!IsReceived && !IsCanceled && !cancellationToken.IsCancellationRequested && waitTime < timeoutMs)
            {
                await Task.Delay(CheckIntervalMs, cancellationToken);
                waitTime += CheckIntervalMs;
            }

            if (waitTime < timeoutMs)
                return ReceivedMessages.Dequeue();
            else
            {
                Logger.Debug($"Receiver timed out");
                return null;
            }
        }

        public virtual void Clear()
        {
            ReceivedMessages.Clear();
        }

        public virtual void Cancel()
        {
            Dispose(disposing: true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    MessageService?.Messenger?.Unregister<TMessage>(this);
                }
                IsCanceled = true;
                ReceivedMessages.Clear();
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
