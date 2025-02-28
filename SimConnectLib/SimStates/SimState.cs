using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using Microsoft.FlightSimulator.SimConnect;
using System;

namespace CFIT.SimConnectLib.SimStates
{
    public class SimState(SimStateInfo state, SimStateManager manager, bool isInternal) : SimResource<SimStateManager, SimState, SimStateSubscription>(state.Name, state.EventId, manager, isInternal)
    {
        public virtual SimStateInfo Info { get; } = state;
        public virtual SimStateUpdate UpdateType { get { return Info.UpdateType; } }
        public virtual SimStateData DataType { get { return Info.DataType; } }
        public override bool IsNumeric { get { return DataType == SimStateData.INT || DataType == SimStateData.FLOAT; } }
        public override bool IsString { get { return DataType == SimStateData.STRING; } }
        public override bool IsStruct { get { return false; } }
        public virtual bool PollEvent { get; protected set; } = false;

        public override void Register()
        {
            if (UpdateType == SimStateUpdate.SUBSCRIBE || UpdateType == SimStateUpdate.BOTH)
            {
                Call(sc => sc.SubscribeToSystemEvent(Id, Name));
            }
            IsRegistered = true;
        }

        public override void Request()
        {
            if (UpdateType == SimStateUpdate.SUBSCRIBE)
            {
                Logger.Warning($"Can not request Value for State '{Name}'");
                return;
            }

            try
            {
                Call(sc => sc.RequestSystemState(Id, Name));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public override void Unregister(bool disconnect)
        {
            if ((UpdateType == SimStateUpdate.SUBSCRIBE || UpdateType == SimStateUpdate.BOTH) && Manager.IsReceiveRunning && IsRegistered)
            {
                Call(sc => sc.UnsubscribeFromSystemEvent(Id));
            }
            IsRegistered = false;
            IsReceived = false;
        }

        protected override bool SetStore(object evtData)
        {
            PollEvent = false;

            if (DataType == SimStateData.STRING && evtData is SIMCONNECT_RECV_EVENT_FILENAME evtFile)
                ValueStore = evtFile.szFileName;
            else if (evtData is SIMCONNECT_RECV_SYSTEM_STATE evtRequest)
                SetStore(evtRequest);
            else if (evtData is SIMCONNECT_RECV_EVENT_OBJECT_ADDREMOVE evtObject)
                ValueStore = (uint)evtObject.eObjType;
            else if (DataType == SimStateData.INT && evtData is SIMCONNECT_RECV_EVENT evt)
                ValueStore = evt.dwData;
            else
            {
                Logger.Warning($"Could not determine Event Structure for SystemState '{Name}'");
                return false;
            }

            return true;
        }

        protected virtual void SetStore(SIMCONNECT_RECV_SYSTEM_STATE evtData)
        {
            if (DataType == SimStateData.STRING)
                ValueStore = evtData.szString;
            else if (DataType == SimStateData.FLOAT)
                ValueStore = evtData.fFloat;
            else if (DataType == SimStateData.INT)
                ValueStore = evtData.dwInteger;
            else
                Logger.Warning($"Could not determine DataType for SystemState '{Name}'");
            PollEvent = true;
        }

        public override bool WriteValue(object value)
        {
            return false;
        }

        public override string ToString()
        {
            return ValueStore?.ToString();
        }
    }
}
