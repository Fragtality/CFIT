using CFIT.SimConnectLib.SimResources;

namespace CFIT.SimConnectLib.Modules.MobiFlight
{
    public partial class MobiVarSubscription(MobiVar resource) : SimResourceSubscription<MobiModule, MobiVar, MobiVarSubscription>(resource)
    {
        //public override void Update()
        //{
        //    if (!IsChanged && !CompareEqual())
        //    {
        //        Logger.Debug($"Value for Variable '{Resource.Name}' changed");
        //        IsChanged = true;
        //        Callback();
        //    }
        //    SetLastValue();
        //}

        //protected virtual bool CompareEqual()
        //{
        //    return LastValue == Value;
        //}

        protected override bool BlockCallback()
        {
            return false;
        }
    }
}
