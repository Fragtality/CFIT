using CFIT.AppFramework.AppConfig;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Threading;

namespace CFIT.AppFramework.MessageService
{
    public class AppMessageService(CancellationToken token) : IDisposable
    {
        public virtual CancellationToken Token { get; } = token;
        public virtual StrongReferenceMessenger Messenger { get; } = new();
        protected bool isDisposed = false;

        public virtual MessageReceiver<TMessage> CreateReceiver<TMessage>(int checkIntervalMs = ProductDefinitionBase.MessageCheckIntervalMs) where TMessage : AppMessage
        {
            return new MessageReceiver<TMessage>(this, checkIntervalMs);
        }

        public virtual TMessage Send<TMessage>(TMessage message) where TMessage : AppMessage
        {
            return Messenger.Send<TMessage>(message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
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
