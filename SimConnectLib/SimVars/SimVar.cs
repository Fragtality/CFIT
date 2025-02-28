using CFIT.AppLogger;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using Microsoft.FlightSimulator.SimConnect;
using System;


namespace CFIT.SimConnectLib.SimVars
{
    public class SimVar(string name, MappedID id, SimUnitType type, SimVarManager manager, bool isInternal) : SimResource<SimVarManager, SimVar, SimVarSubscription>(name, id, manager, isInternal)
    {
        public virtual SimUnitType Type { get; } = type;
        public override bool IsNumeric { get { return Type.IsNumeric(); } }
        public override bool IsString { get { return Type.IsString(); } }
        public override bool IsStruct { get { return Type.IsStruct(); } }

        public SimVar(string name, MappedID id, SimVarManager manager, bool isInternal) : this(name, id, new SimUnitType(SimUnitType.Number), manager, isInternal)
        {

        }

        public override void Register()
        {
            Call(sc => sc.AddToDataDefinition(Id, Name, Type.GetDefinitionName(), Type.DataType, 0, 0));
            Type.RegisterDefineStruct(Id, Manager.Manager);
            Manager.AddDataDefinition(Id);

            Call(sc => sc.RequestDataOnSimObject(Id, Id, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0));

            IsRegistered = true;
            if (Manager.Manager.Config.VerboseLogging)
                Logger.Verbose($"Variable '{Name}' ({Type}) registered on SimConnect with ID '{Id}'");
        }

        public override void Request()
        {

        }

        public override void Unregister(bool disconnect)
        {
            if (Manager.IsReceiveRunning && IsRegistered)
            {
                Call(sc => sc.RequestDataOnSimObject(Id, Id, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.NEVER, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0));
                Call(sc => sc.ClearDataDefinition(Id));
                Manager.ClearDataDefinition(Id);
            }

            IsRegistered = false;
            IsReceived = false;
            if (Manager.Manager.Config.VerboseLogging)
                Logger.Verbose($"Variable '{Name}' ({Type}) with ID '{Id}' unregistered from SimConnect");
        }

        protected override T StructConverter<T>()
        {
            if (typeof(T) == typeof(StructXYZ) && Type.CastType == SimCastType.STRUCT_XYZ)
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(StructPBH) && Type.CastType == SimCastType.STRUCT_PBH)
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(StructLatLonAlt) && Type.CastType == SimCastType.STRUCT_LLA)
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(StructLatLonAltPBH) && Type.CastType == SimCastType.STRUCT_LLAPBH)
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(StructPID) && Type.CastType == SimCastType.STRUCT_PID)
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(StructFuelLevels) && Type.CastType == SimCastType.STRUCT_FUEL)
                return (T)Convert.ChangeType(ValueStore, typeof(T));

            if (typeof(T) == typeof(double))
            {
                if (ValueStore is StructXYZ structXYZ)
                    return (T)Convert.ChangeType(structXYZ.x, typeof(double));
                else if (ValueStore is StructPBH structPBH)
                    return (T)Convert.ChangeType(structPBH.pitch, typeof(double));
                else if (ValueStore is StructLatLonAlt structLLA)
                    return (T)Convert.ChangeType(structLLA.lat, typeof(double));
                else if (ValueStore is StructLatLonAltPBH structLLAPBH)
                    return (T)Convert.ChangeType(structLLAPBH.lat, typeof(double));
                else if (ValueStore is StructPID structPID)
                    return (T)Convert.ChangeType(structPID.pid_p, typeof(double));
                else if (ValueStore is StructFuelLevels structFuel)
                    return (T)Convert.ChangeType(structFuel.Center, typeof(double));
            }

            if (typeof(T) == typeof(string))
                return (T)Convert.ChangeType(ValueStore.ToString(), typeof(string));

            return default;
        }

        public override void SetValue(object value)
        {
            if (value is StructString @struct)
                base.SetValue(@struct.str);
            else
                base.SetValue(value);
        }

        public override bool WriteValue(object value)
        {
            lock (_lock)
            {
                try
                {
                    if (value == null)
                    {
                        Logger.Warning($"Passed value is null");
                        return false;
                    }

                    ValueStore = value;
                    if (IsString && value is string strValue)
                        value = new StructString() { str = strValue };
                    else if (IsNumeric)
                    {
                        if (Type.CastType == SimCastType.DOUBLE)
                            value = Convert.ChangeType(value, typeof(double));
                        else if (Type.CastType == SimCastType.BOOL)
                            value = Convert.ChangeType(value, typeof(bool));
                        else if (Type.CastType == SimCastType.INT)
                            value = Convert.ChangeType(value, typeof(int));
                        else if (Type.CastType == SimCastType.LONG)
                            value = Convert.ChangeType(value, typeof(long));
                        else if (Type.CastType == SimCastType.FLOAT)
                            value = Convert.ChangeType(value, typeof(float));
                    }
                    Logger.Debug($"Writing to Variable '{Name}' - Value: {value}");
                    Call(sc => sc.SetDataOnSimObject(Id, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, value));
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

            if (Type.CastType == SimCastType.STRING)
                result = ValueStore as string;
            else if (Type.CastType >= SimCastType.STRUCT_XYZ)
                result = ValueStore.ToString();
            else if (Type.CastType == SimCastType.BOOL)
                result = GetValue<bool>().ToString();
            else if (Type.CastType == SimCastType.INT && Type.CastType == SimCastType.LONG)
                result = ValueStore.ToString();
            else if (Type.CastType == SimCastType.FLOAT)
                result = GetValue<float>().ToString("F7");
            else if (Type.CastType == SimCastType.DOUBLE)
                result = GetValue<double>().ToString("F9");
            else if (ValueStore != null)
                result = ValueStore.ToString();
            else
                result = "NULL";

            return result;
        }
    }
}
