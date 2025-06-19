using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Threading.Tasks;

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

        public virtual async Task<bool> Call(Action<SimConnect> action)
        {
            return await Manager?.Call(action);
        }

        public virtual Task OnOpen(SIMCONNECT_RECV_OPEN evtData)
        {
            return Task.CompletedTask;
        }

        public abstract void RegisterModule();
        public abstract Task UnregisterModule(bool disconnect);
        public abstract Task CheckState();
        public abstract Task<int> CheckResources();
        public abstract Task ClearUnusedResources(bool clearAll);
    }
}
