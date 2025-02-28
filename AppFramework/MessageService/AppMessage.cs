using CFIT.AppTools;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CFIT.AppFramework.MessageService
{
    public class AppMessage(AppMessageData value) : ValueChangedMessage<AppMessageData>(value)
    {
        public static TMessage Create<TMessage, TSender, TData>(TSender sender, TData data = null) where TMessage : AppMessage where TSender : class where TData : class
        {
            return Create<TMessage, AppMessageData, TSender, TData>(sender, data);
        }

        public static TMessage Create<TMessage, TMessageData, TSender, TData>(TSender sender, TData data = null)
            where TMessage : AppMessage
            where TMessageData : AppMessageData
            where TSender : class
            where TData : class
        {
            var messageData = typeof(TMessageData).CreateInstance<TMessageData, TSender, TData>(sender, data);
            return typeof(TMessage).CreateInstance<TMessage, TMessageData>(messageData);
        }
    }
}
