using CFIT.SimConnectLib.SimResources;

namespace CFIT.SimConnectLib.Modules.MobiFlight
{
    public partial class MobiVarSubscription(MobiVar resource) : SimResourceSubscription<MobiModule, MobiVar, MobiVarSubscription>(resource)
    {
        protected override bool BlockCallback()
        {
            return false;
        }
    }
}
