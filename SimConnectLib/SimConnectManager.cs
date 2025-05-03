using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.InputEvents;
using CFIT.SimConnectLib.Modules;
using CFIT.SimConnectLib.SimEvents;
using CFIT.SimConnectLib.SimStates;
using CFIT.SimConnectLib.SimVars;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace CFIT.SimConnectLib
{
    public partial class SimConnectManager : IDisposable
    {
        public static Mutex SimConnectMutex { get; } = new();
        public CancellationToken Token { get; }
        public virtual ISimConnectConfig Config { get; }
        protected virtual SimConnect SimConnectInstance { get; set; }
        public virtual SIMCONNECT_RECV_OPEN SimConnectOpenData { get; protected set; }
        public virtual SimConnectHook WindowHook { get; }
        internal virtual IdAllocator IdAllocator { get; }
        protected virtual ConcurrentDictionary<string, SimConnectModule> Modules { get; } = [];
        public virtual SimVarManager VariableManager { get; }
        public virtual SimEventManager EventManager { get; }
        public virtual SimStateManager StateManager { get; }
        public virtual InputEventManager InputManager { get; }

        public virtual bool IsSimConnected { get; protected set; } = false;
        public virtual bool IsSimConnectInitialized { get; protected set; } = false;
        public virtual string SimVersionString => SimConnectOpenData != null ? $"{SimConnectOpenData?.szApplicationName} AppVersion {SimConnectOpenData?.dwApplicationVersionMajor}.{SimConnectOpenData?.dwApplicationVersionMinor} AppBuild {SimConnectOpenData?.dwApplicationBuildMajor}.{SimConnectOpenData?.dwApplicationBuildMinor} SimConnectVersion {SimConnectOpenData?.dwSimConnectVersionMajor}.{SimConnectOpenData?.dwSimConnectVersionMinor} SimConnectBuild {SimConnectOpenData?.dwSimConnectBuildMajor}.{SimConnectOpenData?.dwSimConnectBuildMinor}" : "";
        public virtual bool IsReceiveRunning { get; protected set; } = false;
        public virtual bool QuitReceived { get; protected set; } = false;
        protected virtual bool IsDisposed { get; set; } = false;
        public virtual PAUSE_FLAGS PauseFlag { get; protected set; } = PAUSE_FLAGS.PAUSE;
        public virtual bool IsPaused { get; protected set; } = true;
        public virtual bool IsSimRunning { get; protected set; } = false;
        public virtual bool IsCameraValid { get { return CheckCameraReady(); } }
        protected virtual bool LastCameraValid { get; set; } = false;
        public virtual long CameraState { get; protected set; } = 11;
        public virtual string AircraftString { get; protected set; } = "";
        protected virtual bool IsAircraftRequested { get; set; } = false;
        protected virtual bool IsAircraftReceived { get; set; } = false;
        protected virtual bool IsAircraftCleared { get; set; } = true;

        public virtual bool IsSessionStarted { get { return CheckCameraReadyLegacy() && IsSimRunning && IsReceiveRunning; } }
        public virtual bool IsSessionRunning { get { return IsReceiveRunning && IsCameraValid && LastCameraValid && IsSimRunning; } }
        public virtual bool IsSessionStopped { get { return !IsCameraValid || (IsPaused && !IsSimRunning); } }

        protected virtual SimConnect.RecvOpenEventHandler RecvOpenEventHandler { get; }
        protected virtual SimConnect.RecvQuitEventHandler RecvQuitEventHandler { get; }
        protected virtual SimConnect.RecvExceptionEventHandler RecvExceptionEventHandler { get; }
        protected virtual SimConnect.RecvEventEventHandler RecvEventEventHandler { get; }
        protected virtual SimConnect.RecvEventEx1EventHandler RecvEventEx1EventHandler { get; }
        protected virtual SimConnect.RecvEventFilenameEventHandler RecvEventFilenameEventHandler { get; }
        protected virtual SimConnect.RecvSystemStateEventHandler RecvSystemStateEventHandler { get; }
        protected virtual SimConnect.RecvSimobjectDataEventHandler RecvSimobjectDataEventHandler { get; }

        public event Action<SimConnectManager> CallbackWindowHooked;
        public event Action<SimConnectManager, long> CallbackCameraState;
        public event Action<SimConnectManager, bool> CallbackSimState;
        public event Action<SimConnectManager, string> CallbackAircraftString;
        public event Action<SimConnectManager, PAUSE_FLAGS> CallbackPause;
        public event Action<SimConnectManager> CallbackResetState;

        public SimConnectManager(ISimConnectConfig config, Type allocatorType, CancellationToken token)
        {
            Logger.Information($"CFIT.AppLogger Version: {AppLogger.LibVersion.Version}");
            Logger.Information($"CFIT.AppTools Version: {AppTools.LibVersion.Version}");
            Logger.Information($"CFIT.SimConnectManager Version: {LibVersion.Version}");

            RecvOpenEventHandler = new SimConnect.RecvOpenEventHandler(Handler_OnOpen);
            RecvQuitEventHandler = new SimConnect.RecvQuitEventHandler(Handler_OnQuit);
            RecvExceptionEventHandler = new SimConnect.RecvExceptionEventHandler(Handler_OnException);
            RecvEventEventHandler = new SimConnect.RecvEventEventHandler(Handler_OnReceiveEvent);
            RecvEventEx1EventHandler = new SimConnect.RecvEventEx1EventHandler(Handler_OnReceiveEventEx1);
            RecvEventFilenameEventHandler = new SimConnect.RecvEventFilenameEventHandler(Handler_OnReceiveEventFile);
            RecvSystemStateEventHandler = new SimConnect.RecvSystemStateEventHandler(Handler_OnReceiveSystemState);
            RecvSimobjectDataEventHandler = new SimConnect.RecvSimobjectDataEventHandler(Handler_OnSimobjectData);

            Config = config;
            Token = token;
            WindowHook = new(this, Config.MsgSimConnect, Config.MsgConnectRequest);

            IdAllocator = allocatorType.CreateInstance<IdAllocator, uint>(Config.IdBase) ?? throw new Exception("Could not create IdAllocator");
            VariableManager = AddModule(typeof(SimVarManager), null) as SimVarManager ?? throw new Exception("Could not create SimVarManager");
            EventManager = AddModule(typeof(SimEventManager), null) as SimEventManager ?? throw new Exception("Could not create SimEventManager");
            StateManager = AddModule(typeof(SimStateManager), null) as SimStateManager ?? throw new Exception("Could not create SimStateManager");
            InputManager = AddModule(typeof(InputEventManager), null) as InputEventManager ?? throw new Exception("Could not create InputEventManager");

            CreateInternalSubscriptions();
        }

        protected virtual void CreateInternalSubscriptions()
        {
            var camSub = VariableManager.Subscribe("CAMERA STATE", SimUnitType.Enum, true);
            camSub.OnReceived += (sub, value) => { SetCamera(sub.GetValue<uint>()); };

            var aircraftSub = StateManager.Subscribe(SimStateInfo.AircraftLoaded, true);
            aircraftSub.OnReceived += (sub, value) => { SetAircraftState(sub.GetString()); };

            var pauseSub = StateManager.Subscribe(SimStateInfo.Pause_EX1, true);
            pauseSub.OnReceived += (sub, value) => { SetPause(sub.GetValue<uint>()); };

            var simSub = StateManager.Subscribe(SimStateInfo.Sim, true);
            simSub.OnReceived += (sub, value) => { SetSimState(sub.GetValue<uint>() == 1); };
        }

        public virtual void SetSimConnect(SimConnect instance)
        {
            SimConnectInstance = instance;
        }

        public virtual SimConnect GetSimConnect()
        {
            return SimConnectInstance;
        }

        public virtual void CreateMessageHook()
        {
            if (!WindowHook.IsHooked)
            {
                if (Config.CreateWindow)
                {
                    Logger.Information($"Create Window for Message Hook ...");
                    WindowHook.CreateMainWindow();
                }
                else
                {
                    Logger.Information($"Hooking to current Main Window ...");
                    WindowHook.HookMainWindow();
                }
                _ = TaskTools.RunLogged(() => { CallbackWindowHooked?.Invoke(this); }, Token);
            }
        }

        public virtual SimConnectModule AddModule(Type moduleType, object moduleParams)
        {
            try
            {
                if (Modules.ContainsKey(moduleType.Name))
                {
                    Logger.Warning($"SimConnectModule Type '{moduleType.Name}' is already added");
                    return null;
                }

                var module = moduleType.CreateInstance<SimConnectModule, SimConnectManager, object>(this, moduleParams);

                if (module != null)
                {
                    Modules.Add(moduleType.Name, module);
                    Logger.Debug($"Added new Module of Type '{moduleType?.Name}'");
                    if (IsReceiveRunning)
                        module.OnOpen(SimConnectOpenData);
                }
                return module;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }

        public virtual void RemoveModule(Type moduleType)
        {
            try
            {
                if (Modules.TryGetValue(moduleType.Name, out SimConnectModule? module))
                {
                    module?.UnregisterModule(true);
                    Modules.Remove(moduleType.Name);
                    Logger.Debug($"Removed Module of Type '{moduleType?.Name}'");
                }
                else
                    Logger.Warning($"SimConnectModule Type '{moduleType.Name}' is not registered");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual bool Connect()
        {
            try
            {
                if (QuitReceived)
                    return false;

                if (IsSimConnected)
                    return true;

                if (!WindowHook.IsHooked)
                {
                    Logger.Warning($"Message Hook not created - abort");
                    return false;
                }

                if (SimConnectInstance == null)
                {
                    WindowHook.SendConnectMessage();
                }

                if (SimConnectInstance != null && !IsSimConnectInitialized)
                {
                    IsReceiveRunning = false;
                    SimConnectInstance.OnRecvOpen += RecvOpenEventHandler;
                    SimConnectInstance.OnRecvQuit += RecvQuitEventHandler;
                    SimConnectInstance.OnRecvException += RecvExceptionEventHandler;
                    SimConnectInstance.OnRecvEvent += RecvEventEventHandler;
                    SimConnectInstance.OnRecvEventEx1 += RecvEventEx1EventHandler;
                    SimConnectInstance.OnRecvEventFilename += RecvEventFilenameEventHandler;
                    SimConnectInstance.OnRecvSystemState += RecvSystemStateEventHandler;
                    SimConnectInstance.OnRecvSimobjectData += RecvSimobjectDataEventHandler;
                    IsSimConnectInitialized = true;
                    Logger.Debug($"SimConnect Object initialized");
                }

                return SimConnectInstance != null && IsSimConnectInitialized;
            }
            catch (Exception ex)
            {
                ResetState();
                Logger.LogException(ex);
            }

            return false;
        }

        protected virtual void SetConnected()
        {
            if (!IsSimConnected)
            {
                Logger.Information($"Sim Connection fully established.");
                IsSimConnected = true;
            }
        }

        public virtual void Disconnect()
        {
            try
            {
                CallModules(module => { module.UnregisterModule(true); });

                if (SimConnectInstance != null)
                {
                    try
                    {
                        SimConnectInstance.OnRecvOpen -= RecvOpenEventHandler;
                        SimConnectInstance.OnRecvQuit -= RecvQuitEventHandler;
                        SimConnectInstance.OnRecvException -= RecvExceptionEventHandler;
                        SimConnectInstance.OnRecvEvent -= RecvEventEventHandler;
                        SimConnectInstance.OnRecvEventEx1 -= RecvEventEx1EventHandler;
                        SimConnectInstance.OnRecvEventFilename -= RecvEventFilenameEventHandler;
                        SimConnectInstance.OnRecvSystemState -= RecvSystemStateEventHandler;
                        SimConnectInstance.OnRecvSimobjectData -= RecvSimobjectDataEventHandler;
                    }
                    catch { }

                    try { SimConnectInstance.Dispose(); } catch { }
                    SimConnectInstance = null;
                }
                ResetState();

                Logger.Information($"SimConnect Connection closed.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void SetCamera(uint state)
        {
            Logger.Debug($"Received Camera State: {state}");
            CameraState = state;
            _ = TaskTools.RunLogged(() => { CallbackCameraState?.Invoke(this, CameraState); }, Token);
        }

        protected virtual void SetAircraftState(string str)
        {
            IsAircraftReceived = !string.IsNullOrWhiteSpace(str);
            Logger.Debug($"Received AircraftString: {str}");
            AircraftString = str;
            IsAircraftRequested = false;
            _ = TaskTools.RunLogged(() => { CallbackAircraftString?.Invoke(this, AircraftString); }, Token);
        }

        protected virtual void SetPause(uint value)
        {
            PauseFlag = (PAUSE_FLAGS)value;
            Logger.Debug($"Received Pause: {PauseFlag as Enum}");
            if (PauseFlag.HasFlag(PAUSE_FLAGS.PAUSE) || PauseFlag.HasFlag(PAUSE_FLAGS.PAUSE_ACTIVE) || PauseFlag.HasFlag(PAUSE_FLAGS.PAUSE_SIM))
            {
                Logger.Information("Sim is paused");
                IsPaused = true;
            }
            else if (value == 0)
            {
                Logger.Information("Sim is unpaused");
                IsPaused = false;
            }
            _ = TaskTools.RunLogged(() => { CallbackPause?.Invoke(this, PauseFlag); }, Token);
        }

        protected virtual void SetSimState(bool simRunning)
        {
            if (!IsSimRunning && simRunning)
            {
                Logger.Debug($"SimState true");
                IsSimRunning = true;
                _ = TaskTools.RunLogged(() => { CallbackSimState?.Invoke(this, IsSimRunning); }, Token);
            }
            if (IsSimRunning && !simRunning)
            {
                Logger.Debug($"SimState false");
                IsSimRunning = false;
                _ = TaskTools.RunLogged(() => { CallbackSimState?.Invoke(this, IsSimRunning); }, Token);
            }
        }

        protected virtual void ResetState()
        {
            SimConnectInstance = null;
            IsSimConnectInitialized = false;
            IsSimConnected = false;
            IsPaused = true;
            IsReceiveRunning = false;
            IsSimRunning = false;
            CameraState = 11;
            AircraftString = "";
            IsAircraftReceived = false;
            IsAircraftRequested = false;
            IsAircraftCleared = true;
            _ = TaskTools.RunLogged(() => { CallbackResetState?.Invoke(this); }, Token);
        }


        public virtual bool Call(Action<SimConnect> action)
        {
            bool result = false;
            if (!IsSimConnectInitialized || SimConnectInstance == null)
                return result;

            try
            {
                SimConnectMutex.TryWaitOne();
                action?.Invoke(SimConnectInstance);
                SimConnectMutex.ReleaseMutex();
                result = true;
            }
            catch (Exception ex)
            {
                SimConnectMutex.TryReleaseMutex();
                if (!QuitReceived)
                    Logger.LogException(ex);
                else
                    Logger.Warning($"{ex.GetType().Name} during SimConnect Call");
            }

            return result;
        }

        internal virtual void ReceiveMessage()
        {
            try
            {
                SimConnectMutex.TryWaitOne();
                SimConnectInstance?.ReceiveMessage();
                SimConnectMutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                SimConnectMutex.TryReleaseMutex();
                IsReceiveRunning = false;

                if (ex.Message != "0xC00000B0")
                    Logger.LogException(ex);
                else
                    Logger.Error($"Exception catched: '{ex.GetType()}' - '{ex.Message}'");
            }
        }

        protected virtual bool IsNotCamStateUnpaused(long value)
        {
            return CameraState != value || (CameraState == value && IsPaused && !string.IsNullOrWhiteSpace(AircraftString));
        }

        public virtual bool CheckCameraReady()
        {
            return CameraState != 0 && IsNotCamStateUnpaused(32) && CameraState != 35 && (CameraState < 11 || CameraState >= 29 || CameraState == 26);
        }

        public virtual bool CheckCameraReadyLegacy()
        {
            return !IsPaused && ((CameraState != 0 && CameraState < 11) || (IsAircraftReceived && (CameraState == 30 || CameraState == 31)));
        }

        protected virtual void CallModules(Action<SimConnectModule> action)
        {
            foreach (var module in Modules)
            {
                try
                {
                    action.Invoke(module.Value);
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Module: {module.Key}");
                    Logger.LogException(ex);
                }
            }
        }

        protected virtual void CheckAircraftString()
        {
            if (string.IsNullOrWhiteSpace(AircraftString) && IsSessionStarted)
            {
                if (StateManager.HasName(SimStateInfo.AircraftLoaded, out uint id) && StateManager.GetResource(id, out SimState state) && !state.IsReceived)
                {
                    Logger.Debug($"Request AircraftString");
                    state.Request();
                    IsAircraftRequested = true;
                }
                else
                    Logger.Warning($"Can not request AircraftString!");
            }
            else if (!string.IsNullOrWhiteSpace(AircraftString) && IsSessionStarted && IsAircraftCleared)
            {
                IsAircraftCleared = false;
                Logger.Debug($"Reset String cleared state");
            }
            else if (IsSessionStopped && !IsAircraftCleared)
            {
                IsAircraftCleared = true;
                Logger.Debug($"Clear AircraftString");
                SetAircraftState("");
                IsAircraftReceived = false;
            }
        }

        public virtual void CheckState()
        {
            if (IsReceiveRunning)
            {
                CheckAircraftString();
                CallModules(module => { module.CheckState(); });
                LastCameraValid = IsCameraValid;
            }
        }

        public virtual int CheckResources()
        {
            int count = 0;
            if (IsReceiveRunning)
                CallModules(module => { count += module.CheckResources(); });

            return count;
        }

        public virtual void ClearUnusedRessources(bool clearAll)
        {
            CallModules(module => module.ClearUnusedResources(clearAll));
        }

        public event Action<SIMCONNECT_RECV_OPEN> OnOpen;
        protected virtual void Handler_OnOpen(SimConnect sender, SIMCONNECT_RECV_OPEN evtData)
        {
            try
            {
                if (SimConnectInstance != null)
                {
                    IsReceiveRunning = true;
                    SimConnectOpenData = evtData;
                    Logger.Information($"SimConnect OnOpen received: {SimVersionString}");
                    try { OnOpen?.Invoke(evtData); } catch (Exception ex) { Logger.LogException(ex); }
                }
                else
                    Logger.Error("SimConnect is NULL!");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public event Action<SIMCONNECT_RECV> OnQuit;
        protected virtual void Handler_OnQuit(SimConnect sender, SIMCONNECT_RECV evtData)
        {
            Logger.Information($"SimConnect OnQuit received.");
            QuitReceived = true;
            try { OnQuit?.Invoke(evtData); } catch (Exception ex) { Logger.LogException(ex); }
            Disconnect();
        }

        public event Action<SIMCONNECT_RECV_EXCEPTION> OnException;
        protected virtual void Handler_OnException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION evtData)
        {
            Logger.Error($"Exception '{((SIMCONNECT_EXCEPTION)evtData.dwException) as Enum}' received: (dwException {evtData.dwException} | dwID {evtData.dwID} | dwSendID {evtData.dwSendID} | dwIndex {evtData.dwIndex})");
            try { OnException?.Invoke(evtData); } catch { }
        }

        public event Action<SIMCONNECT_RECV_SIMOBJECT_DATA> OnSimobjectData;
        protected virtual void Handler_OnSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA evtData)
        {
            if (Config.VerboseLogging)
                Logger.Verbose($"OnSimobjectData: dwID {evtData.dwID} | dwRequestID {evtData.dwRequestID} | dwDefineID {evtData.dwDefineID} | dwObjectID {evtData.dwObjectID}");

            try { OnSimobjectData?.Invoke(evtData); } catch (Exception ex) { Logger.LogException(ex); }
        }

        public event Action<SIMCONNECT_RECV_SYSTEM_STATE> OnReceiveSystemState;
        protected virtual void Handler_OnReceiveSystemState(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE evtData)
        {
            if (Config.VerboseLogging)
                Logger.Verbose($"OnReceiveSystemState: dwID {evtData.dwID} | dwRequestID {evtData.dwRequestID} | dwInteger {evtData.dwInteger} | fFloat {evtData.fFloat} | szString {evtData.szString}");

            try { OnReceiveSystemState?.Invoke(evtData); } catch (Exception ex) { Logger.LogException(ex); }
        }

        public event Action<SIMCONNECT_RECV_EVENT_FILENAME> OnReceiveEventFile;
        protected virtual void Handler_OnReceiveEventFile(SimConnect sender, SIMCONNECT_RECV_EVENT_FILENAME evtData)
        {
            if (Config.VerboseLogging)
                Logger.Verbose($"OnReceiveEventFile: dwID {evtData.dwID} | uGroupID {evtData.uGroupID} | uEventID {evtData.uEventID} | szFileName {evtData.szFileName}");

            try { OnReceiveEventFile?.Invoke(evtData); } catch (Exception ex) { Logger.LogException(ex); }
        }

        public event Action<SIMCONNECT_RECV_EVENT_EX1> OnReceiveEventEx1;
        protected virtual void Handler_OnReceiveEventEx1(SimConnect sender, SIMCONNECT_RECV_EVENT_EX1 evtData)
        {
            SetConnected();
            if (Config.VerboseLogging)
                Logger.Verbose($"OnReceiveEventEx1: dwID {evtData.dwID} | uGroupID {evtData.uGroupID} | uEventID {evtData.uEventID} | dwData0 {evtData.dwData0} | dwData1 {evtData.dwData1} | dwData2 {evtData.dwData2} | dwData3 {evtData.dwData3} | dwData4 {evtData.dwData4}");

            try { OnReceiveEventEx1?.Invoke(evtData); } catch (Exception ex) { Logger.LogException(ex); }
        }

        public event Action<SIMCONNECT_RECV_EVENT> OnReceiveEvent;
        protected virtual void Handler_OnReceiveEvent(SimConnect sender, SIMCONNECT_RECV_EVENT evtData)
        {
            SetConnected();
            if (Config.VerboseLogging)
                Logger.Verbose($"OnReceiveEvent: dwID {evtData.dwID} | uGroupID {evtData.uGroupID} | uEventID {evtData.uEventID} | dwData {evtData.dwData}");

            try { OnReceiveEvent?.Invoke(evtData); } catch (Exception ex) { Logger.LogException(ex); }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (IsSimConnectInitialized)
                        Disconnect();
                    WindowHook?.ClearHook();
                }
                IsDisposed = true;
            }
        }

        public virtual void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
