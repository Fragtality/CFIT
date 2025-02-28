using CFIT.AppLogger;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using Microsoft.FlightSimulator.SimConnect;
using System;

namespace CFIT.SimConnectLib.InputEvents
{
    public class InputEvent(string name, ulong hash, MappedID id, InputEventManager manager, SimCastType type = SimCastType.DOUBLE)
        : SimResource<InputEventManager, InputEvent, InputEventSubscription>(name, id, manager, false)
    {
        public virtual ulong Hash { get; } = hash;
        public virtual SimCastType Type { get; } = type;
        public override bool IsNumeric { get { return Type == SimCastType.DOUBLE; } }
        public override bool IsString { get { return Type == SimCastType.STRING; } }
        public override bool IsStruct { get { return false; } }

        public override void Register()
        {
            Call(sc => sc.SubscribeInputEvent(Hash));

            IsRegistered = true;
            if (Manager.Manager.Config.VerboseLogging)
                Logger.Verbose($"InputEvent '{Name}' ({Type}) registered on SimConnect");
        }

        public override void Request()
        {
            if (Manager.IsReceiveRunning)
            {
                if (Manager.Manager.Config.VerboseLogging)
                    Logger.Verbose($"Requesting InputEvent '{Name}'");
                Call(sc => sc.GetInputEvent(Id, Hash));
            }
        }

        public override void Unregister(bool disconnect)
        {
            if (Manager.IsReceiveRunning && IsRegistered)
                Call(sc => sc.UnsubscribeInputEvent(Hash));

            IsRegistered = false;
            IsReceived = false;
            if (Manager.Manager.Config.VerboseLogging)
                Logger.Verbose($"InputEvent '{Name}' ({Type}) unregistered from SimConnect");
        }

        protected override bool SetStore(object value)
        {
            if (Type == SimCastType.STRING)
                ValueStore = (value as SimConnect.InputEventString)?.value ?? "";
            else
                ValueStore = (double)value;

            return true;
        }

        public override bool WriteValue(object value)
        {
            lock (_lock)
            {
                try
                {
                    if (value == null)
                    {
                        Logger.Warning($"Value is null");
                        return false;
                    }

                    SetStore(value);
                    Logger.Debug($"Writing to InputEvent '{Name}' - Value: {ToString()}");
                    Call(sc => sc.SetInputEvent(Hash, ValueStore));
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
            string result;

            if (Type == SimCastType.STRING && ValueStore is string valueString)
                result = valueString;
            else if (Type == SimCastType.DOUBLE && ValueStore is double valueDouble)
                result = valueDouble.ToString("F9");
            else if (ValueStore != null)
                result = ValueStore.ToString();
            else
                result = "NULL";

            return result;
        }
    }
}
