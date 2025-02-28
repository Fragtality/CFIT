using CFIT.AppFramework.AppConfig;
using CFIT.AppFramework.MessageService;
using CFIT.AppTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CFIT.AppFramework.ResourceStores
{
    public class ReceiverStore(AppMessageService messageService)
    {
        protected virtual AppMessageService MessageService { get; } = messageService;
        protected virtual ConcurrentDictionary<Type, object> Receivers { get; } = [];
        protected virtual ConcurrentDictionary<Type, int> RefCount { get; } = [];
        public virtual int Count { get { return Receivers.Count; } }

        public virtual MessageReceiver<TMessage> Add<TMessage>(int checkIntervalMs = ProductDefinitionBase.MessageCheckIntervalMs) where TMessage : AppMessage
        {
            var type = typeof(TMessage);
            if (!TryGet<TMessage>(out MessageReceiver<TMessage> receiver))
            {
                receiver = new MessageReceiver<TMessage>(MessageService, checkIntervalMs);
                Receivers.Add(type, receiver);
                RefCount.Add(type, 1);
            }
            else
                RefCount[type] = RefCount[type] + 1;

            return receiver;
        }

        public virtual MessageReceiver<TMessage> Remove<TMessage>() where TMessage : AppMessage
        {
            var type = typeof(TMessage);
            if (TryGet<TMessage>(out MessageReceiver<TMessage> receiver))
            {
                if (RefCount[type] <= 1)
                {
                    receiver.Dispose();
                    Receivers.Remove(type);
                    RefCount.Remove(type);
                }
                else
                    RefCount[type] = RefCount[type] - 1;
            }

            return receiver;
        }

        public virtual void Clear()
        {
            foreach (var receiver in Receivers.Values)
                (receiver as IDisposable).Dispose();
            Receivers.Clear();
        }

        public virtual MessageReceiver<TMessage> Get<TMessage>() where TMessage : AppMessage
        {
            if (TryGet(out MessageReceiver<TMessage> receiver))
                return receiver;
            else
                return null;
        }

        public virtual bool Contains<TMessage>() where TMessage : AppMessage
        {
            return Receivers.ContainsKey(typeof(TMessage));
        }

        public virtual bool TryGet<TMessage>(out MessageReceiver<TMessage> receiver) where TMessage : AppMessage
        {
            receiver = null;
            if (Receivers.TryGetValue(typeof(TMessage), out object storedReceiver))
            {
                receiver = storedReceiver as MessageReceiver<TMessage>;
                return true;
            }
            else
                return false;
        }

        public virtual bool RegisterEventHandler<TMessage>(Action<TMessage> eventHandler) where TMessage : AppMessage
        {
            if (TryGet<TMessage>(out MessageReceiver<TMessage> receiver))
            {
                receiver.OnMessage += eventHandler;
                return true;
            }

            return false;
        }

        public virtual bool UnregisterEventHandler<TMessage>(Action<TMessage> eventHandler) where TMessage : AppMessage
        {
            if (TryGet<TMessage>(out MessageReceiver<TMessage> receiver))
            {
                receiver.OnMessage -= eventHandler;
                return true;
            }

            return false;
        }
    }
}
