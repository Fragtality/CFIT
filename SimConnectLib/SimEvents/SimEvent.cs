using CFIT.AppLogger;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using Microsoft.FlightSimulator.SimConnect;
using System;

namespace CFIT.SimConnectLib.SimEvents
{
    public class SimEvent(string name, MappedID id, MappedID groupId, SimEventManager manager, bool isInternal) : SimResource<SimEventManager, SimEvent, SimEventSubscription>(name, id, manager, isInternal)
    {
        public virtual uint[] EventValues { get; } = new uint[5];
        public virtual MappedID GroupId { get; } = groupId;
        public override object ValueStore { get { return EventValues[0]; } protected set { EventValues[0] = (uint)value; } }
        public override bool IsNumeric { get { return true; } }
        public override bool IsString { get { return false; } }
        public override bool IsStruct { get { return false; } }
        public virtual bool HasMultipleParams { get; protected set; } = false;

        public override void Register()
        {
            Call(sc => sc.MapClientEventToSimEvent(Id, Name));
            Call(sc => sc.AddClientEventToNotificationGroup(GroupId, Id, false));

            IsRegistered = true;
            if (Manager.Manager.Config.VerboseLogging)
                Logger.Verbose($"Event '{Name}' registered on SimConnect with ID '{Id}'");
        }

        public override void Request()
        {
            
        }

        public override void Unregister(bool disconnect)
        {
            if (Manager.IsReceiveRunning && IsRegistered)
                Call(sc => sc.RemoveClientEvent(GroupId, Id));

            IsRegistered = false;
            IsReceived = false;
            if (Manager.Manager.Config.VerboseLogging)
                Logger.Verbose($"Event '{Name}' with ID '{Id}' unregistered from SimConnect");
        }

        protected override bool SetStore(object value)
        {
            return SetStore(value, 0);
        }

        protected virtual bool SetStore(object value, int index)
        {
            EventValues[index] = (uint)Convert.ChangeType(value, typeof(uint));
            HasMultipleParams = index > 0;
            return true;
        }

        public override void SetValues(object[] values)
        {
            lock (_lock)
            {
                try
                {
                    for (int i = 0; i < values.Length && i < EventValues.Length; i++)
                    {
                        if (!SetStore(values[i], i))
                            return;
                    }
                    SetReceived();
                    NotifySubscribers();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        public override bool WriteValue(object value)
        {
            return WriteValues([value]);
        }

        public override bool WriteValues(object[] values)
        {
            SetValues(values);
            return Write();
        }

        protected virtual bool Write()
        {
            lock (_lock)
            {
                try
                {
                    if (EventValues == null || EventValues?.Length == 0)
                    {
                        Logger.Warning($"Illegal Value Count - null {EventValues == null} len {EventValues?.Length}");
                        return false;
                    }

                    Logger.Debug($"Writing to Event '{Name}' - Values: {string.Join(',', EventValues)}");
                    if (HasMultipleParams)
                        Call(sc => sc.TransmitClientEvent_EX1(SimConnect.SIMCONNECT_OBJECT_ID_USER, Id, GroupId, SIMCONNECT_EVENT_FLAG.DEFAULT,
                            EventValues[0], EventValues[1], EventValues[2], EventValues[3], EventValues[4]));
                    else
                        Call(sc => sc.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, Id, EventValues[0], GroupId, SIMCONNECT_EVENT_FLAG.DEFAULT));
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            if (HasMultipleParams && EventValues != null)
                return string.Join(',', EventValues);
            else if (!HasMultipleParams && EventValues != null)
                return EventValues[0].ToString();
            else
                return ValueStore?.ToString();
        }
    }
}
