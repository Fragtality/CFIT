using CFIT.SimConnectLib.SimResources;

namespace CFIT.SimConnectLib.SimVars
{
    public partial class SimVarSubscription(SimVar variable) : SimResourceSubscription<SimVarManager, SimVar, SimVarSubscription>(variable)
    {
        protected override bool BlockCallback()
        {
            return false;
        }
    }
}
