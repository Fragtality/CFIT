using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CFIT.SimConnectLib.Modules.MobiFlight
{
    public class MobiModule : SimResourceManager<MobiModule, MobiVar, MobiVarSubscription>
    {
        public const string MOBIFLIGHT_CHANNEL_NAME_COMMAND = "MobiFlight.Command";
        public const string MOBIFLIGHT_CHANNEL_NAME_RESPONSE = "MobiFlight.Response";
        public const uint MOBIFLIGHT_MESSAGE_SIZE = 1024;
        public const uint MOBIFLIGHT_STRINGVAR_SIZE = 128;
        public const uint MOBIFLIGHT_STRINGVAR_MAX_AMOUNT = 64;
        public const uint MOBIFLIGHT_STRINGVAR_DATAAREA_SIZE = MOBIFLIGHT_STRINGVAR_SIZE * MOBIFLIGHT_STRINGVAR_MAX_AMOUNT;

        public virtual string ClientName { get { return Manager.Config.ClientName; } }
        public virtual string CLIENT_CHANNEL_NAME_COMMAND { get { return $"{ClientName}.Command"; } }
        public virtual string CLIENT_CHANNEL_NAME_RESPONSE { get { return $"{ClientName}.Response"; } }
        public virtual string CLIENT_CHANNEL_NAME_SIMVAR { get { return $"{ClientName}.LVars"; } }
        public virtual string CLIENT_CHANNEL_NAME_STRINGVAR { get { return $"{ClientName}.StringVars"; } }
        public virtual MappedID CLIENT_ID_MOBIMODULE { get; }
        public virtual MappedID CLIENT_ID_MOBICLIENT { get; }
        public virtual MappedID DATA_ID_MOBIFLIGHT_CMD { get; }
        public virtual MappedID DATA_ID_MOBIFLIGHT_RESPONSE { get; }
        public virtual MappedID DATA_ID_MOBIFLIGHT_SIMVARS { get; }
        public virtual MappedID DATA_ID_MOBIFLIGHT_STRINGVARS { get; }
        public virtual MappedID DATA_ID_CLIENT_CMD { get; }
        public virtual MappedID DATA_ID_CLIENT_RESPONSE { get; }
        public virtual MappedID DATA_ID_CLIENT_SIMVARS { get; }
        public virtual MappedID DATA_ID_CLIENT_STRINGVARS { get; }

        public virtual IMobiConfig Config { get; protected set; }
        public virtual SimConnectManager SimConnect { get; }
        protected virtual MappedIdStore ClientDataIdStore { get; set; }
        protected virtual bool AreaModuleCreated { get; set; } = false;
        protected virtual bool AreaClientCreated { get; set; } = false;
        protected virtual MappedIdStore ClientIdStore { get; set; }
        protected virtual MappedIdStore VarIdStore { get { return IdStore; } }
        protected virtual MappedIdStore StringVarIdStore { get; set; }
        public virtual bool IsMobiConnected { get; protected set; } = false;
        protected virtual DateTime LastConnectionAttempt { get; set; } = DateTime.MinValue;
        public virtual List<string> LvarList { get; } = [];
        protected virtual bool RequestingList { get; set; } = false;

        protected virtual SimConnect.RecvClientDataEventHandler RecvClientDataEventHandler { get; }

        public MobiModule(SimConnectManager manager, object config) : base(manager, config)
        {
            Config = config as IMobiConfig ?? throw new Exception($"Could not cast passed Config to IMobiConfig ('{config?.GetType()?.Name}')");
            RecvClientDataEventHandler = new SimConnect.RecvClientDataEventHandler(OnClientData);

            CLIENT_ID_MOBIMODULE = ClientIdStore.MapConstant("CLIENT_ID_MOBIMODULE");
            CLIENT_ID_MOBICLIENT = ClientIdStore.MapConstant("CLIENT_ID_MOBICLIENT");

            DATA_ID_MOBIFLIGHT_CMD = ClientDataIdStore.MapConstant("DATA_ID_MOBIFLIGHT_CMD");
            DATA_ID_MOBIFLIGHT_RESPONSE = ClientDataIdStore.MapConstant("DATA_ID_MOBIFLIGHT_RESPONSE");
            DATA_ID_MOBIFLIGHT_SIMVARS = ClientDataIdStore.MapConstant("DATA_ID_MOBIFLIGHT_SIMVARS");
            DATA_ID_MOBIFLIGHT_STRINGVARS = ClientDataIdStore.MapConstant("DATA_ID_MOBIFLIGHT_STRINGVARS");
            DATA_ID_CLIENT_CMD = ClientDataIdStore.MapConstant("DATA_ID_CLIENT_CMD");
            DATA_ID_CLIENT_RESPONSE = ClientDataIdStore.MapConstant("DATA_ID_CLIENT_RESPONSE");
            DATA_ID_CLIENT_SIMVARS = ClientDataIdStore.MapConstant("DATA_ID_CLIENT_SIMVARS");
            DATA_ID_CLIENT_STRINGVARS = ClientDataIdStore.MapConstant("DATA_ID_CLIENT_STRINGVARS");
        }

        protected override MappedIdStore AllocateStore()
        {
            ClientDataIdStore = Manager.IdAllocator.AllocateStore(10, ID_TYPE.CLIENT_DATA_ID);
            ClientIdStore = Manager.IdAllocator.AllocateStore(10, ID_TYPE.DEFINE_ID, ID_TYPE.DEFINE_ID | ID_TYPE.REQUEST_ID);
            var varIdStore = Manager.IdAllocator.AllocateStore(Config.MobiSizeVariables, ID_TYPE.DEFINE_ID, ID_TYPE.DEFINE_ID | ID_TYPE.REQUEST_ID);
            StringVarIdStore = Manager.IdAllocator.AllocateStore(Config.MobiSizeVariables, ID_TYPE.DEFINE_ID, ID_TYPE.DEFINE_ID | ID_TYPE.REQUEST_ID);
            return varIdStore;
        }

        protected override void SetModuleParams(object moduleParams)
        {
            Config = moduleParams as IMobiConfig;
        }

        public override void RegisterModule()
        {
            Manager.OnOpen += OnOpen;
        }

        public override void OnOpen(SIMCONNECT_RECV_OPEN evtData)
        {
            base.OnOpen(evtData);
            Manager.GetSimConnect().OnRecvClientData += RecvClientDataEventHandler;
            CreateDataAreaDefaultChannel();
        }

        protected virtual void CheckConnection()
        {
            try
            {
                if (IsReceiveRunning && !IsMobiConnected && DateTime.Now - LastConnectionAttempt >= TimeSpan.FromMilliseconds(Config.MobiRetryDelay))
                {
                    Logger.Information($"Sending Ping to MobiFlight WASM Module.");
                    ClearLvarList();
                    LastConnectionAttempt = DateTime.Now;
                    SendMobiWasmCmd("MF.DummyCmd");
                    SendMobiWasmCmd("MF.Ping");
                    SendMobiWasmCmd("MF.DummyCmd");
                }
                else if (Manager.IsSessionStarted && LvarList.Count == 0 && Config.MobiWriteLvars)
                    GetLvarList();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public override void CheckState()
        {
            CheckConnection();
        }

        public override int CheckResources()
        {
            if (IsMobiConnected)
                return base.CheckResources();
            return 0;
        }

        protected override void Unregister(bool disconnect)
        {
            if (disconnect && Manager.IsReceiveRunning)
            {
                if (IsMobiConnected)
                {
                    try { ClearUnusedResources(true); } catch (Exception ex) { Logger.LogException(ex); }
                    RegisteredDataDefinitions.Clear();
                    IsMobiConnected = false;
                    LastConnectionAttempt = DateTime.MinValue;
                    ClearLvarList();
                    Logger.Information($"MobiModule Connection closed.");
                }

                Manager.GetSimConnect().OnRecvClientData -= RecvClientDataEventHandler;
            }
        }

        protected virtual void CreateDataAreaDefaultChannel()
        {
            if (AreaModuleCreated)
                return;

            Call(sc => sc.MapClientDataNameToID(MOBIFLIGHT_CHANNEL_NAME_COMMAND, DATA_ID_MOBIFLIGHT_CMD));
            Call(sc => sc.MapClientDataNameToID(MOBIFLIGHT_CHANNEL_NAME_RESPONSE, DATA_ID_MOBIFLIGHT_RESPONSE));

            Call(sc => sc.AddToClientDataDefinition(CLIENT_ID_MOBIMODULE, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0));

            Call(sc => sc.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, MobiMessage>(CLIENT_ID_MOBIMODULE));

            Call(sc => sc.RequestClientData(DATA_ID_MOBIFLIGHT_RESPONSE,
                CLIENT_ID_MOBIMODULE,
                CLIENT_ID_MOBIMODULE,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0));

            AreaModuleCreated = true;
        }

        protected virtual void CreateDataAreaClientChannel()
        {
            if (AreaClientCreated)
                return;

            Call(sc => sc.MapClientDataNameToID(CLIENT_CHANNEL_NAME_COMMAND, DATA_ID_CLIENT_CMD));
            Call(sc => sc.MapClientDataNameToID(CLIENT_CHANNEL_NAME_RESPONSE, DATA_ID_CLIENT_RESPONSE));
            Call(sc => sc.MapClientDataNameToID(CLIENT_CHANNEL_NAME_SIMVAR, DATA_ID_CLIENT_SIMVARS));
            Call(sc => sc.MapClientDataNameToID(CLIENT_CHANNEL_NAME_STRINGVAR, DATA_ID_CLIENT_STRINGVARS));

            Call(sc => sc.AddToClientDataDefinition(CLIENT_ID_MOBICLIENT, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0));

            Call(sc => sc.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, MobiMessage>(CLIENT_ID_MOBICLIENT));

            Call(sc => sc.RequestClientData(DATA_ID_CLIENT_RESPONSE,
                CLIENT_ID_MOBICLIENT,
                CLIENT_ID_MOBICLIENT,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0));

            AreaClientCreated = true;
        }

        protected virtual void OnClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA evtData)
        {
            try
            {
                if (Manager.Config.VerboseLogging)
                    Logger.Verbose($"dwRequestID {evtData.dwRequestID} dwDefineID {evtData.dwDefineID} dwentrynumber {evtData.dwentrynumber} dwObjectID {evtData.dwObjectID} dwID {evtData.dwID}");

                if (evtData.dwRequestID == CLIENT_ID_MOBIMODULE && evtData?.dwData?.Length > 0)
                {
                    OnModuleRequest((MobiMessage)evtData.dwData[0]);
                }
                else if (evtData.dwRequestID == CLIENT_ID_MOBICLIENT && evtData?.dwData?.Length > 0)
                {
                    OnClientRequest((MobiMessage)evtData.dwData[0]);
                }
                else if (Resources.TryGetValue(evtData.dwRequestID, out MobiVar variable) && (evtData?.dwData[0] is MobiVarValue || evtData ?.dwData[0] is MobiStringValue))
                {
                    variable.SetValue(evtData.dwData[0]);
                }
                else
                {
                    Logger.Warning($"Received unknown Event! (dwID {evtData?.dwID} | dwDefineID {evtData?.dwDefineID} | dwRequestID {evtData?.dwRequestID} | dwData {evtData?.dwData[0]?.GetType().Name})");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void OnModuleRequest(MobiMessage request)
        {
            if (request == "MF.Pong")
            {
                if (!IsMobiConnected)
                {
                    Logger.Information($"MobiFlight WASM Ping acknowledged - opening Client Connection.");
                    SendMobiWasmCmd("MF.DummyCmd");
                    SendMobiWasmCmd($"MF.Clients.Add.{ClientName}");
                    SendMobiWasmCmd("MF.DummyCmd");
                }
                else
                    Logger.Debug($"MF.Pong received although already connected.");
            }
            else if (request == $"MF.Clients.Add.{ClientName}.Finished")
            {
                CreateDataAreaClientChannel();
                IsMobiConnected = true;
                LastConnectionAttempt = DateTime.MinValue;
                SendClientWasmDummyCmd();
                SendClientWasmCmd("MF.SimVars.Clear");
                if (Config.MobiSetVarPerFrame)
                {
                    Logger.Information($"Setting Mobi Vars Per Frame to {Config.MobiVarsPerFrame}");
                    SendClientWasmCmd($"MF.Config.MAX_VARS_PER_FRAME.Set.{Config.MobiVarsPerFrame}");
                }
                Logger.Information($"MobiFlight WASM Client Connection opened.");
            }
            else if (!request.Data.StartsWith("MF.Clients.Add."))
                Logger.Information($"Unhandled MobiFlight Messages received: '{request.Data}'");
        }

        protected virtual void OnClientRequest(MobiMessage request)
        {
            if (request.Data == $"MF.LVars.List.Start")
            {
                LvarList.Clear();
                Logger.Debug($"Receiving L-Vars from MF Module ...");
            }
            else if (request.Data == $"MF.LVars.List.End")
            {
                Logger.Debug($"Received all L-Vars from MF Module!");
                try
                {
                    string file = Config.MobiLvarFile;
                    if (File.Exists(file))
                        File.Delete(file);

                    File.WriteAllLines(file, LvarList);
                }
                catch (IOException)
                {
                    Logger.Warning($"Could not write L-Vars to File!");
                }
                RequestingList = false;
            }
            else if (!string.IsNullOrWhiteSpace(request.Data))
            {
                Logger.Verbose($"Received L-Var: {request.Data}");
                LvarList.Add(request.Data);
            }
        }

        public virtual void SendClientWasmCmd(string command, bool includeDummy = true)
        {
            if (Manager.Config.VerboseLogging)
                Logger.Verbose($"command = {command}");
            SendWasmCmd(DATA_ID_CLIENT_CMD, CLIENT_ID_MOBICLIENT, command);
            if (includeDummy)
                SendClientWasmDummyCmd();
        }

        public virtual void SendClientWasmDummyCmd()
        {
            SendWasmCmd(DATA_ID_CLIENT_CMD, CLIENT_ID_MOBICLIENT, "MF.DummyCmd");
        }

        public virtual void SendMobiWasmCmd(string command)
        {
            SendWasmCmd(DATA_ID_MOBIFLIGHT_CMD, CLIENT_ID_MOBIMODULE, command);
        }

        protected virtual void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
        {
            Call(sc => sc.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new MobiMessageBuffer(command)));
        }

        public virtual void GetLvarList()
        {
            if (!RequestingList && IsMobiConnected)
            {
                RequestingList = true;
                Logger.Debug($"Requesting L-Var List");
                SendClientWasmCmd("MF.LVars.List");
            }
        }

        public virtual void ClearLvarList()
        {
            RequestingList = false;
            LvarList.Clear();
        }

        public override MobiVarSubscription Subscribe(string name, bool isInternal = false)
        {
            return Subscribe(name, SimUnitType.Number);
        }

        public virtual MobiVarSubscription Subscribe(string name, string type)
        {
            Logger.Debug($"Create new Subscription for Variable '{name}' ({type})");
            MobiVar variable;
            name = MobiVar.NameNormalized(name, type);
            if (!HasName(name, out uint id) || Resources[id]?.Type != type)
            {
                MappedID defineId;
                uint offset;
                if (type != SimUnitType.String)
                {
                    defineId = VarIdStore.GetNext();
                    offset = VarIdStore.GetOffset();
                }
                else
                {
                    defineId = StringVarIdStore.GetNext();
                    offset = StringVarIdStore.GetOffset();
                }

                variable = new MobiVar(name, defineId, offset, new SimUnitType(type), this);
                Resources.Add(defineId, variable);
                Logger.Debug($"Added new Variable for '{name}' ({type}) on Id '{defineId}'");
            }
            else
                variable = Resources[id];

            if (variable == null)
            {
                Logger.Warning($"Returned Variable for '{name}' is NULL");
                return null;
            }

            return new MobiVarSubscription(variable);
        }

        public virtual MobiVarSubscription SubscribeCode(string code)
        {
            Logger.Debug($"Create new Subscription for Code '{code}'");
            MobiVar variable;
            if (!HasName(code, out uint id))
            {
                MappedID defineId = VarIdStore.GetNext();
                uint offset = VarIdStore.GetOffset();

                variable = new MobiVar(code, defineId, offset, new SimUnitType(SimUnitType.Number), this);
                Resources.Add(defineId, variable);
                Logger.Debug($"Added new Code-Variable on Id '{defineId}'");
            }
            else
            {
                variable = Resources[id];
                Logger.Debug($"Using existing Code-Variable on Id '{id}'");
            }

            if (variable == null)
            {
                Logger.Warning($"Returned Variable for '{code}' is NULL");
                return null;
            }

            return new MobiVarSubscription(variable);
        }

        public override void ClearUnusedResources(bool clearAll)
        {
            if (!clearAll)
            {
                var unused = Resources.Where(kv => kv.Value.IsRegistered && !kv.Value.IsSubscribed && !kv.Value.IsInternal);
                if (unused.Any())
                {
                    Logger.Debug($"Unregister unused MobiVars: {unused.Count()}");
                    foreach (var simres in unused)
                        simres.Value.Unregister(false);
                }
            }
            else
            {
                Logger.Debug($"Removing all MobiVars: {Resources.Count}");
                SendClientWasmCmd("MF.SimVars.Clear");

                foreach (var mobiVar in Resources)
                    mobiVar.Value.Unregister(false);
                
                Resources.Clear();
                IdStore.Reset();
                StringVarIdStore.Reset();
                SendClientWasmCmd("MF.SimVars.Clear");
            }
        }

        public virtual void SetVariable(string name, string type, string value)
        {
            SendClientWasmCmd($"MF.SimVars.Set.{MobiVar.GetWriteCode(name, type, value)}");
        }

        public virtual bool ExecuteCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            try
            {
                SendClientWasmCmd($"MF.SimVars.Set.{code}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }

            return true;
        }
    }
}
