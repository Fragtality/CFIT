using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using Microsoft.FlightSimulator.SimConnect;
using System.Collections.Generic;
using System.Linq;

namespace CFIT.SimConnectLib.SimStates
{
    public class SimStateManager : SimResourceManager<SimStateManager, SimState, SimStateSubscription>
    {
        public virtual Dictionary<string, SimStateInfo> KnownStates { get; }

        public SimStateManager(SimConnectManager manager, object moduleParams) : base(manager, moduleParams)
        {
            KnownStates = SimStateInfo.CreateStateInfo(IdStore);
        }

        protected override MappedIdStore AllocateStore()
        {
            return Manager.IdAllocator.AllocateStore(Manager.Config.SizeSimStates, ID_TYPE.EVENT_ID);
        }

        public override void RegisterModule()
        {
            Manager.OnReceiveEvent += Update;
            Manager.OnReceiveEventFile += Update;
            Manager.OnReceiveSystemState += Update;
        }

        protected virtual void Update(SIMCONNECT_RECV evtData)
        {
            if (evtData is SIMCONNECT_RECV_SYSTEM_STATE evtState)
                Update(evtState.dwRequestID, evtData);
            else if (evtData is SIMCONNECT_RECV_EVENT_FILENAME evtFile)
                Update(evtFile.uEventID, evtData);
            else if (evtData is SIMCONNECT_RECV_EVENT evtEvent && evtEvent?.uGroupID == MappedID.SYSTEM)
                Update(evtEvent.uEventID, evtData);
            else if (Manager.Config.VerboseLogging)
                Logger.Verbose($"Received Event did not match - dwID {evtData?.dwID} Type {evtData?.GetType()?.Name}");
        }

        protected virtual void Update(uint id, SIMCONNECT_RECV evtData)
        {
            if (GetResource(id, out SimState state))
            {
                state.SetValue(evtData);
            }
            else
                Logger.Warning($"Received unknown EventID/RequestID '{id}' for System State");
        }

        public override SimStateSubscription Subscribe(string name, bool isInternal = false)
        {
            return Subscribe(name, isInternal, 500, false);
        }

        public virtual SimStateSubscription Subscribe(string stateName, bool isInternal = false, int pollInterval = 500, bool pollOnly = false)
        {
            if (KnownStates.TryGetValue(stateName, out SimStateInfo info))
                return Subscribe(info, isInternal, pollInterval, pollOnly);
            else
            {
                Logger.Warning($"The SimState '{stateName}' is not known!");
                return null;
            }
        }

        public virtual SimStateSubscription Subscribe(SimStateInfo info, bool isInternal = false, int pollInterval = 500, bool pollOnly = false)
        {
            if (Manager.Config.VerboseLogging)
                Logger.Verbose($"Create new Subscription for State '{info.Name}'");

            if (!Resources.TryGetValue(info.EventId, out SimState state))
            {
                state = new SimState(info, this, isInternal);
                Resources.Add(info.EventId, state);
                if (Manager.Config.VerboseLogging)
                    Logger.Verbose($"Added new State for '{info.Name}' on Id '{info.EventId}'");
            }

            if (state == null)
            {
                Logger.Warning($"Returned Event for '{info.Name}' is NULL");
                return null;
            }

            return new SimStateSubscription(state, pollInterval, pollOnly);
        }

        public override void CheckState()
        {

        }

        public override int CheckResources()
        {
            int count = base.CheckResources();

            if (count == 0)
            {
                var query = Resources.Where(kv => kv.Value.IsRegistered && !kv.Value.IsReceived && kv.Value.UpdateType != SimStateUpdate.SUBSCRIBE);
                foreach (var kv in query)
                    kv.Value.Request();
                count += query.Count();
            }

            return count;
        }

        protected override void Unregister(bool disconnect)
        {
            
        }

        public override void ClearUnusedResources(bool clearAll)
        {
            if (!clearAll)
            {
                var unused = Resources.Where(kv => kv.Value.IsRegistered && !kv.Value.IsSubscribed && !kv.Value.IsInternal);
                if (unused.Any())
                {
                    Logger.Debug($"Unregister unused SimStates: {unused.Count()}");
                    foreach (var simres in unused)
                        simres.Value.Unregister(false);
                }
            }
            else
            {
                var noninternal = Resources.Where(kv => !kv.Value.IsInternal).Select(kv => kv.Key).ToList();
                Logger.Debug($"Removing all non-internal SimStates: {noninternal.Count}");
                foreach (var key in noninternal)
                    Resources[key].Unregister(true);
                foreach (var key in noninternal)
                    Resources.Remove(key);

                IdStore.Reset();
            }
        }
    }
}
