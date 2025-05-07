using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CFIT.SimConnectLib.InputEvents
{
    public class InputEventManager : SimResourceManager<InputEventManager, InputEvent, InputEventSubscription>
    {
        protected virtual MappedID EnumRequestID { get; }
        public virtual ConcurrentDictionary<ulong, InputEvent> InputEvents { get; } = [];
        protected virtual ConcurrentDictionary<uint, ulong> RequestIds { get; } = [];
        public virtual bool HasEventsEnumerated { get; protected set; } = false;

        public event Action<InputEventManager, bool> CallbackEventsEnumerated;

        protected virtual SimConnect.RecvEnumerateInputEventsEventHandler RecvEnumerateInputEventsEventHandler { get; set; }
        protected virtual SimConnect.RecvGetInputEventEventHandler RecvGetInputEventEventHandler { get; set; }
        protected virtual SimConnect.RecvSubscribeInputEventEventHandler RecvSubscribeInputEventEventHandler { get; set; }

        public InputEventManager(SimConnectManager manager, object moduleParams) : base(manager, moduleParams)
        {
            RecvEnumerateInputEventsEventHandler = new SimConnect.RecvEnumerateInputEventsEventHandler(OnRecvEnumerateInputEvents);
            RecvGetInputEventEventHandler = new SimConnect.RecvGetInputEventEventHandler(OnRecvGetInputEvents);
            RecvSubscribeInputEventEventHandler = new SimConnect.RecvSubscribeInputEventEventHandler(OnRecvSubscribeInputEvent);

            EnumRequestID = IdStore.MapConstant("EnumerateInputEvents");
        }

        protected override MappedIdStore AllocateStore()
        {
            return Manager.IdAllocator.AllocateStore(Manager.Config.SizeInputEvents, ID_TYPE.REQUEST_ID);
        }

        public override void RegisterModule()
        {
            Manager.OnOpen += OnOpen;
        }

        public override void OnOpen(SIMCONNECT_RECV_OPEN evtData)
        {
            Manager.GetSimConnect().OnRecvEnumerateInputEvents += RecvEnumerateInputEventsEventHandler;
            Manager.GetSimConnect().OnRecvGetInputEvent += RecvGetInputEventEventHandler;
            Manager.GetSimConnect().OnRecvSubscribeInputEvent += RecvSubscribeInputEventEventHandler;
        }

        public virtual bool IsNameEnumerated(string name, out ulong hash)
        {
            var query = Resources.Where(kv => kv.Value.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (query.Any() && RequestIds.TryGetValue(query.FirstOrDefault().Key, out hash))
            {
                return true;
            }
            else
            {
                hash = 0;
                return false;
            }
        }

        public override InputEventSubscription Subscribe(string name, bool isInternal = false)
        {
            if (IsNameEnumerated(name, out ulong hash))
                return Subscribe(hash);
            else
            {
                Logger.Warning($"No Hash found for InputEvent '{name}'");
                return null;
            }
        }

        public virtual InputEventSubscription Subscribe(uint id)
        {
            if (!Resources.TryGetValue(id, out InputEvent inputEvent))
            {
                Logger.Warning($"No InputEvent found for ID '{id}'");
                return null;
            }
            else
                return new InputEventSubscription(inputEvent);
        }

        public virtual InputEventSubscription Subscribe(ulong hash)
        {
            if (!InputEvents.TryGetValue(hash, out InputEvent inputEvent))
            {
                Logger.Warning($"No InputEvent found for Hash '{hash}'");
                return null;
            }
            else
                return new InputEventSubscription(inputEvent);
        }

        protected virtual InputEventSubscription Subscribe(InputEvent inputEvent)
        {
            if (Manager.Config.VerboseLogging)
                Logger.Verbose($"Create new Subscription for InputEvent '{inputEvent.Name}' ({inputEvent.Hash})");
            return new InputEventSubscription(inputEvent);
        }

        protected void OnRecvEnumerateInputEvents(SimConnect sender, SIMCONNECT_RECV_ENUMERATE_INPUT_EVENTS evtData)
        {
            try
            {
                if (evtData?.dwRequestID == EnumRequestID)
                {
                    IEnumerable<SIMCONNECT_INPUT_EVENT_DESCRIPTOR> receivedEvents = evtData?.rgData?.Cast<SIMCONNECT_INPUT_EVENT_DESCRIPTOR>() ?? [];
                    Logger.Debug($"Received {receivedEvents?.Count()} InputEvents");
                    foreach (var evt in receivedEvents)
                    {
                        var requestId = IdStore.GetNext();
                        InputEvents.Add(evt.Hash,
                            new(evt.Name?.ToLowerInvariant(), evt.Hash, requestId, this, (evt.eType == SIMCONNECT_INPUT_EVENT_TYPE.STRING ? SimCastType.STRING : SimCastType.DOUBLE)));
                        RequestIds.Add(requestId, evt.Hash);
                        Resources.Add(requestId, InputEvents[evt.Hash]);
                        if (Manager.Config.VerboseLogging)
                            Logger.Verbose($"Event '{evt?.Name}' added - Hash {evt?.Hash} Type {evt?.eType} RequestID {requestId}");
                    }

                    HasEventsEnumerated = true;
                    _ = TaskTools.RunLogged(() => { CallbackEventsEnumerated?.Invoke(this, true); }, Manager.Token);
                }
                else
                    Logger.Debug($"Received Event did not match - dwID {evtData?.dwID} dwRequestID {evtData?.dwRequestID}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void OnRecvGetInputEvents(SimConnect sender, SIMCONNECT_RECV_GET_INPUT_EVENT evtData)
        {
            try
            {
                if (evtData != null && RequestIds.TryGetValue(evtData.dwRequestID, out ulong hash))
                {
                    if (InputEvents.TryGetValue(hash, out InputEvent inputEvent))
                        inputEvent.SetValues(evtData.Value);
                    else
                        Logger.Warning($"No InputEvent found for Hash '{hash}'");
                }
                else
                    Logger.Debug($"Received Event did not match - dwID {evtData?.dwID} dwRequestID {evtData?.dwRequestID} Value.Length {evtData?.Value?.Length}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void OnRecvSubscribeInputEvent(SimConnect sender, SIMCONNECT_RECV_SUBSCRIBE_INPUT_EVENT evtData)
        {
            try
            {
                if (evtData != null && InputEvents.TryGetValue(evtData.Hash, out InputEvent inputEvent))
                    inputEvent.SetValues(evtData.Value);
                else
                    Logger.Warning($"No InputEvent found for Hash '{evtData?.Hash}' - dwID {evtData?.dwID}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void EnumerateInputEvents()
        {
            Logger.Debug($"Sending EnumerateInputEvents Request");
            HasEventsEnumerated = false;
            Call(sc => sc.EnumerateInputEvents(EnumRequestID));
        }

        protected void ClearInputEvents()
        {
            Logger.Debug($"Clearing InputEvents");
            try
            {
                foreach (var inputEvent in InputEvents.Values)
                    inputEvent.Unregister(false);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            InputEvents.Clear();
            Resources.Clear();
            RequestIds.Clear();
            IdStore.Reset();
            HasEventsEnumerated = false;
        }

        public override void CheckState()
        {
            if (!HasEventsEnumerated && Manager.IsSessionStarted)
            {
                EnumerateInputEvents();
            }
            else if (HasEventsEnumerated && Manager.IsSessionStopped)
            {
                ClearInputEvents();
                _ = TaskTools.RunLogged(() => { CallbackEventsEnumerated?.Invoke(this, false); }, Manager.Token);
            }
        }

        public override int CheckResources()
        {
            int count = 0;
            if (HasEventsEnumerated && Manager.IsSessionRunning)
            {
                var query = InputEvents.Where(kv => !kv.Value.IsRegistered && kv.Value.IsSubscribed);
                count = query.Count();
                foreach (var kv in query)
                    kv.Value.Register();

                if (count == 0)
                {
                    query = InputEvents.Where(kv => kv.Value.IsRegistered && !kv.Value.IsReceived);
                    foreach (var kv in query)
                        kv.Value.Request();
                    count += query.Count();
                }
            }

            return count;
        }

        protected override void Unregister(bool disconnect)
        {
            if (disconnect && Manager.IsReceiveRunning)
            {
                Manager.GetSimConnect().OnRecvEnumerateInputEvents -= RecvEnumerateInputEventsEventHandler;
                Manager.GetSimConnect().OnRecvGetInputEvent -= RecvGetInputEventEventHandler;
                Manager.GetSimConnect().OnRecvSubscribeInputEvent -= RecvSubscribeInputEventEventHandler;
            }
        }

        public override void ClearUnusedResources(bool clearAll)
        {
            if (!clearAll)
            {
                var unused = Resources.Where(kv => kv.Value.IsRegistered && !kv.Value.IsSubscribed && !kv.Value.IsInternal);
                if (unused.Any())
                {
                    Logger.Debug($"Unregister unused InputEvents: {unused.Count()}");
                    foreach (var simres in unused)
                        simres.Value.Unregister(false);
                }
            }
            else
            {
                ClearInputEvents();
            }
        }

        public virtual bool SendEvent(string name, double value)
        {
            if (!IsNameEnumerated(name, out ulong hash))
            {
                Logger.Warning($"The Name '{name}' is not enumerated!");
                return false;
            }

            return Call(sc => sc.SetInputEvent(hash, value));
        }
    }
}
