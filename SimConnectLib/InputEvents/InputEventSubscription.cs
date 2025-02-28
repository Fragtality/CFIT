using CFIT.SimConnectLib.SimResources;

namespace CFIT.SimConnectLib.InputEvents
{
    public partial class InputEventSubscription(InputEvent inputEvent) : SimResourceSubscription<InputEventManager, InputEvent, InputEventSubscription>(inputEvent)
    {
        public virtual ulong Hash { get { return Resource.Hash; } }

        protected override bool ChangeCondition()
        {
            return true;
        }

        protected override bool BlockCallback()
        {
            return false;
        }
    }
}
