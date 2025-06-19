using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Threading.Tasks;

namespace CFIT.SimConnectLib.Modules.MobiFlight
{
    public class MobiVar : SimResource<MobiModule, MobiVar, MobiVarSubscription>
    {
        public virtual MappedID ChannelID { get; }
        public virtual uint Offset { get; }
        public virtual uint Size { get; }
        public virtual SimUnitType Type { get; }
        public override bool IsNumeric { get { return Type.IsNumeric(); } }
        public override bool IsString { get { return Type.IsString(); } }
        public override bool IsStruct { get { return false; } }

        public MobiVar(string name, MappedID id, uint offset, SimUnitType type, MobiModule manager) : base(name, id, manager, false)
        {
            Offset = offset;
            Type = type;
            Size = (type.IsString() ? MobiModule.MOBIFLIGHT_STRINGVAR_SIZE : sizeof(float));
            ChannelID = (type.IsString() ? Manager.DATA_ID_CLIENT_STRINGVARS : Manager.DATA_ID_CLIENT_SIMVARS);
        }

        public MobiVar(string name, MappedID id, uint offset, MobiModule manager) : this(name, id, offset, new SimUnitType(SimUnitType.Number), manager)
        {
            
        }

        public static string NameNormalized(string name, string type = "number")
        {
            if (string.IsNullOrWhiteSpace(name))
                return "(L:NULL,number)";

            if (string.IsNullOrWhiteSpace(type))
                type = "number";
            else
                type = type.ToLowerInvariant();

            string prefix = "";
            if (!name.StartsWith("L:") && !name.StartsWith("A:"))
                prefix = "A:";

            return $"({prefix}{name},{type})";
        }

        public override async Task Register()
        {
            if (Manager.AddDataDefinition(Id))
            {
                await Call(sc => sc.AddToClientDataDefinition(
                    Id,
                    Offset * Size,
                    Size,
                    0,
                    0));
                Logger.Debug($"AddToClientDataDefinition {Id}");
            }

            if (Type.IsString())
                await Call(sc => sc.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, MobiStringValue>(Id));
            else
                await Call(sc => sc.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, MobiVarValue>(Id));

            await Call(sc => sc.RequestClientData(
                ChannelID,
                Id,
                Id,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0
            ));

            if (Type.IsString())
                await Manager.SendClientWasmCmd($"MF.SimVars.AddString.{Name}");
            else
                await Manager.SendClientWasmCmd($"MF.SimVars.Add.{Name}");

            IsRegistered = true;
            if (Manager.Manager.Config.VerboseLogging)
                Logger.Verbose($"Variable '{Name}' registered on MobiFlight with ID '{Id}' (Channel {ChannelID})");
        }

        public override Task Request()
        {
            return Task.CompletedTask;
        }

        public override async Task Unregister(bool disconnect)
        {
            if (Manager.IsMobiConnected && IsRegistered)
            {
                await Call(sc => sc.RequestClientData(
                    ChannelID,
                    Id,
                    Id,
                    SIMCONNECT_CLIENT_DATA_PERIOD.NEVER,
                    SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                    0,
                    0,
                    0
                    ));
            }

            if (disconnect && Manager.ClearDataDefinition(Id))
            {
                Logger.Debug($"Manager.ClearDataDefinition {Id}");
                await Call(sc => sc.ClearClientDataDefinition(Id));
            }

            IsRegistered = false;
            IsReceived = false;
            if (Manager.Manager.Config.VerboseLogging)
                Logger.Verbose($"Variable '{Name}' with ID '{Id}' unregistered from MobiFlight");
        }

        protected override bool SetStore(object value)
        {
            if (value is MobiVarValue structFloat)
            {
                ValueStore = structFloat.data;
            }
            else if (value is MobiStringValue structString)
            {
                ValueStore = structString.data;
            }
            else
            {
                Logger.Warning($"Passed struct has wrong Type '{value?.GetType()?.Name}'");
                return false;
            }

            return true;
        }

        public static string GetWriteCode(string name, string type, string value)
        {
            string code = NameNormalized(name, type).Insert(1, ">");
            if (string.IsNullOrWhiteSpace(value))
                value = "0";

            if (type == SimUnitType.String)
                return $"'{value}' {code}";
            else
                return $"{value} {code}";
        }

        protected virtual string GetWriteCode(string name, string value)
        {
            string code = name.Insert(1, ">");
            if (string.IsNullOrWhiteSpace(value))
                value = "0";

            if (Type == SimUnitType.String)
                return $"'{value}' {code}";
            else
                return $"{value} {code}";
        }

        public override async Task<bool> WriteValue(object value)
        {
            try
            {
                if (value == null)
                {
                    Logger.Warning($"Passed value is null");
                    return false;
                }


                string writeValue = null;
                if (Conversion.CanCast<float>(value as object, out float fValue))
                    writeValue = Conversion.ToString(fValue);
                else if (Conversion.CanCast<int>(value as object, out int iValue))
                    writeValue = Conversion.ToString(iValue);
                else if (value is string strValue)
                    writeValue = strValue;

                if (writeValue != null)
                {
                    await _lock.WaitAsync();
                    await Manager.SendClientWasmCmd($"MF.SimVars.Set.{GetWriteCode(Name, writeValue)}");
                    _lock.Release();
                    return true;
                }
                else
                    Logger.Warning($"Could not convert Value '{value}' ({value?.GetType()?.Name})");
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
            return false;
        }

        public override string ToString()
        {
            string result;

            if (Type.CastType == SimCastType.STRING && ValueStore is string valueString)
                result = valueString;
            else if (Type.CastType == SimCastType.DOUBLE)
                if (ValueStore is float @float)
                    result = @float.ToString("F7");
                else if (ValueStore is double @double)
                    result = @double.ToString("F9");
                else
                    result = ValueStore.ToString();
            else
                result = "NULL";

            return result;
        }
    }
}
