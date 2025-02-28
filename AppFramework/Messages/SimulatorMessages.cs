using CFIT.AppFramework.MessageService;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.SimResources;

namespace CFIT.AppFramework.Messages
{
    public class MessageDataSim(SimConnectManager manager, ISimResourceSubscription sub = null) : AppMessageData(manager, sub)
    {
        public virtual SimConnectManager SimConnect { get { return Sender as SimConnectManager; } }
        public virtual ISimResourceSubscription Subscription { get { return Data as ISimResourceSubscription; } }
    }

    public class MessageSimulator(MessageDataSim value) : AppMessage(value)
    {
        public virtual MessageDataSim Data { get { return Value as MessageDataSim; } }

        public static TMessage Create<TMessage>(SimConnectManager manager, ISimResourceSubscription sub = null) where TMessage : MessageSimulator
        {
            return Create<TMessage, MessageDataSim, SimConnectManager, ISimResourceSubscription>(manager, sub);
        }
    }

    public class MsgSimStarted(MessageDataSim value) : MessageSimulator(value) { }
    
    public class MsgSimStopped(MessageDataSim value) : MessageSimulator(value) { }
    
    public class MsgSessionReady(MessageDataSim value) : MessageSimulator(value) { }

    public class MsgSessionEnded(MessageDataSim value) : MessageSimulator(value) { }
}
