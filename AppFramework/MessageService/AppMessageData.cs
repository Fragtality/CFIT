using System;

namespace CFIT.AppFramework.MessageService
{
    public class AppMessageData(object sender, object data = null)
    {
        public virtual object Sender { get; } = sender;
        public virtual Type DataType { get { return Data?.GetType(); } }
        public virtual bool HasData { get { return Data != null; } }
        public virtual object Data { get; } = data;
        public virtual T GetData<T>() where T : class
        {
            return Data as T;
        }
    }
}
