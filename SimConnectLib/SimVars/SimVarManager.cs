using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using Microsoft.FlightSimulator.SimConnect;
using System.Linq;
using System.Threading.Tasks;

namespace CFIT.SimConnectLib.SimVars
{
    public class SimVarManager(SimConnectManager manager, object moduleParams) : SimResourceManager<SimVarManager, SimVar, SimVarSubscription>(manager, moduleParams)
    {
        protected override MappedIdStore AllocateStore()
        {
            return Manager.IdAllocator.AllocateStore(Manager.Config.SizeVariables, ID_TYPE.DEFINE_ID, ID_TYPE.DEFINE_ID | ID_TYPE.REQUEST_ID);
        }

        public override void RegisterModule()
        {
            Manager.OnSimobjectData += Update;
        }

        public override Task CheckState()
        {
            return Task.CompletedTask;
        }

        protected override Task Unregister(bool disconnect)
        {
            return Task.CompletedTask;
        }

        protected virtual void Update(SIMCONNECT_RECV_SIMOBJECT_DATA evtData)
        {
            if (IdStore.Contains(evtData?.dwDefineID) && evtData?.dwData?.Length >= 1)
                Update(evtData.dwRequestID, evtData.dwData[0]);
            else
                Logger.Debug($"Received Event did not match - dwDefineID {evtData?.dwDefineID} dwRequestID {evtData?.dwRequestID} dwData.Length {evtData?.dwData?.Length}");
        }

        protected virtual void Update(uint id, object value)
        {
            if (GetResource(id, out SimVar variable))
            {
                variable.SetValue(value);
            }
            else
                Logger.Warning($"Received unknown RequestID '{id}' on DefineID for Dynamic Vars");
        }

        public override SimVarSubscription Subscribe(string name, bool isInternal = false)
        {
            return Subscribe(name, SimUnitType.Number, isInternal);
        }

        public virtual SimVarSubscription Subscribe(string name, string type, bool isInternal = false)
        {
            if (Manager.Config.VerboseLogging)
                Logger.Verbose($"Create new Subscription for Variable '{name}' ({type})");
            SimVar variable;
            if (!HasName(name, out uint id) || Resources[id]?.Type != type)
            {
                MappedID defineId;
                if (isInternal)
                    defineId = IdStore.MapConstant(name);
                else
                    defineId = IdStore.GetNext();

                variable = new SimVar(name, defineId, new SimUnitType(type), this, isInternal);
                Resources.Add(defineId, variable);
                if (Manager.Config.VerboseLogging)
                    Logger.Verbose($"Added new Variable for '{name}' ({type}) on Id '{defineId}'");
            }
            else
                variable = Resources[id];

            if (variable == null)
            {
                Logger.Warning($"Returned Variable for '{name}' is NULL");
                return null;
            }

            return new SimVarSubscription(variable);
        }

        public override async Task ClearUnusedResources(bool clearAll)
        {
            if (!clearAll)
            {
                var unused = Resources.Where(kv => kv.Value.IsRegistered && !kv.Value.IsSubscribed && !kv.Value.IsInternal);
                if (unused.Any())
                {
                    Logger.Debug($"Unregister unused SimVars: {unused.Count()}");
                    foreach (var simres in unused)
                        await simres.Value.Unregister(false);
                }
            }
            else
            {
                var noninternal = Resources.Where(kv => !kv.Value.IsInternal).Select(kv => kv.Key).ToList();
                Logger.Debug($"Removing all non-internal SimVars: {noninternal.Count}");
                foreach (var key in noninternal)
                    await Resources[key].Unregister(true);
                foreach (var key in noninternal)
                    Resources.Remove(key);

                IdStore.Reset();
                RegisteredDataDefinitions.Clear();
                foreach (var id in Resources.Keys)
                    RegisteredDataDefinitions.Add(id);
            }
        }
    }
}
