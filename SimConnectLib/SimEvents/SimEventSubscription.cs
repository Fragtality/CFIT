using CFIT.SimConnectLib.SimResources;

namespace CFIT.SimConnectLib.SimEvents
{
    public partial class SimEventSubscription(SimEvent @event) : SimResourceSubscription<SimEventManager, SimEvent, SimEventSubscription>(@event)
    {
        public virtual uint[] EventValues { get { return Resource.EventValues; } }

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
