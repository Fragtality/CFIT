using Microsoft.FlightSimulator.SimConnect;
using System;

namespace CFIT.SimConnectLib.Modules
{
    public abstract class SimConnectModule
    {
        public virtual SimConnectManager Manager { get; }
        public virtual bool IsReceiveRunning { get { return Manager.IsReceiveRunning; } }

        public SimConnectModule(SimConnectManager manager, object moduleParams)
        {
            Manager = manager;
            SetModuleParams(moduleParams);
            RegisterModule();
        }

        protected abstract void SetModuleParams(object moduleParams);

        public virtual bool Call(Action<SimConnect> action)
        {
            return Manager?.Call(action) ?? false;
        }

        public virtual void OnOpen(SIMCONNECT_RECV_OPEN evtData)
        {

        }

        public abstract void RegisterModule();
        public abstract void UnregisterModule(bool disconnect);
        public abstract void CheckState();
        public abstract int CheckResources();
        public abstract void ClearUnusedResources(bool clearAll);
    }
}
