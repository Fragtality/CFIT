using CFIT.AppFramework.MessageService;

namespace CFIT.AppFramework.Messages
{
    public class MessageSimulator() : AppMessage() { }
    public class MsgSimStarted() : MessageSimulator() { }

    public class MsgSimStopped() : MessageSimulator() { }

    public class MsgSessionReady() : MessageSimulator() { }

    public class MsgSessionEnded() : MessageSimulator() { }
}