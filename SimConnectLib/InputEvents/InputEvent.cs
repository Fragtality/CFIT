using CFIT.AppLogger;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Threading.Tasks;

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

        public override async Task Register()
        {
            await Call(sc => sc.SubscribeInputEvent(Hash));

            IsRegistered = true;
            if (Manager.Manager.Config.VerboseLogging)
                Logger.Verbose($"InputEvent '{Name}' ({Type}) registered on SimConnect");
        }

        public override async Task Request()
        {
            if (Manager.IsReceiveRunning)
            {
                if (Manager.Manager.Config.VerboseLogging)
                    Logger.Verbose($"Requesting InputEvent '{Name}'");
                await Call(sc => sc.GetInputEvent(Id, Hash));
            }
        }

        public override async Task Unregister(bool disconnect)
        {
            if (Manager.IsReceiveRunning && IsRegistered)
                await Call(sc => sc?.UnsubscribeInputEvent(Hash));

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

        public override async Task<bool> WriteValue(object value)
        {
            try
            {
                if (value == null)
                {
                    Logger.Warning($"Value is null");
                    return false;
                }

                SetStore(value);
                await _lock.WaitAsync();
                Logger.Debug($"Writing to InputEvent '{Name}' - Value: {ToString()}");
                await Call(sc => sc.SetInputEvent(Hash, ValueStore));
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
            finally
            {
                try { _lock.Release(); } catch { }
            }
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
