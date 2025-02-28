using CFIT.AppFramework.AppConfig;
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
                while (IsReceived && !IsCanceled && !Token.IsCancellationRequested)
                    OnMessage?.Invoke(ReceivedMessages.Dequeue());
            }, Token);
        }

        public async Task<TMessage> ReceiveAsync(bool clearQueue = false)
        {
            if (clearQueue)
                Clear();

            while (!IsReceived && !IsCanceled && !Token.IsCancellationRequested)
            {
                await Task.Delay(CheckIntervalMs, Token);
            }

            return ReceivedMessages.Dequeue();
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
