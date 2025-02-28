using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using Microsoft.FlightSimulator.SimConnect;
using System.Collections.Concurrent;
using System.Linq;

namespace CFIT.SimConnectLib.SimEvents
{
    public class SimEventManager(SimConnectManager manager, object moduleParams) : SimResourceManager<SimEventManager, SimEvent, SimEventSubscription>(manager, moduleParams)
    {
        public virtual MappedID EVENT_GROUP_SUBSCRIBED { get; } = new(1);
        public virtual MappedID EVENT_GROUP_COMMAND { get; } = new(2);
        protected virtual ConcurrentDictionary<string, SimEventSubscription> SendEvents { get; } = [];

        protected override MappedIdStore AllocateStore()
        {
            return Manager.IdAllocator.AllocateStore(Manager.Config.SizeEvents, ID_TYPE.EVENT_ID); ;
        }

        public override void RegisterModule()
        {
            Manager.OnReceiveEvent += Update;
            Manager.OnReceiveEventEx1 += Update;
        }

        protected virtual void Update(SIMCONNECT_RECV evt)
        {
            if (evt is SIMCONNECT_RECV_EVENT evtData && (evtData.uGroupID == EVENT_GROUP_SUBSCRIBED || evtData.uGroupID == EVENT_GROUP_COMMAND))
                UpdateId(evtData.uEventID, evtData.dwData);
            else if (evt is SIMCONNECT_RECV_EVENT_EX1 evtDataEx && (evtDataEx.uGroupID == EVENT_GROUP_SUBSCRIBED || evtDataEx.uGroupID == EVENT_GROUP_COMMAND))
                UpdateId(evtDataEx.uEventID, evtDataEx.dwData0, evtDataEx.dwData1, evtDataEx.dwData2, evtDataEx.dwData3, evtDataEx.dwData4);
            else if (evt is SIMCONNECT_RECV_EVENT || evt is SIMCONNECT_RECV_EVENT_EX1)
            {
                uint groupId = (evt is SIMCONNECT_RECV_EVENT_EX1 ? (evt as SIMCONNECT_RECV_EVENT_EX1).uGroupID : (evt as SIMCONNECT_RECV_EVENT)?.uGroupID ?? 0);
                if (groupId != MappedID.SYSTEM && groupId != EVENT_GROUP_COMMAND)
                {
                    uint eventId = (evt is SIMCONNECT_RECV_EVENT_EX1 ? (evt as SIMCONNECT_RECV_EVENT_EX1).uEventID : (evt as SIMCONNECT_RECV_EVENT)?.uEventID ?? 0);
                    Logger.Debug($"Received Event did not match - dwID {evt?.dwID} uGroupID {groupId} uEventID {eventId}");
                }
            }
            else
                Logger.Debug($"Received Event had wrong Type - dwID {evt?.dwID} Type {evt?.GetType()?.Name}");
        }

        protected virtual void UpdateId(uint id, params object[] values)
        {
            if (GetResource(id, out SimEvent @event))
            {
                @event.SetValues(values);
            }
            else
                Logger.Warning($"Received unknown EventID '{id}' on NotifyID for Dynamic Events");
        }

        public override SimEventSubscription Subscribe(string name, bool isInternal = false)
        {
            return Subscribe(name, EVENT_GROUP_SUBSCRIBED, isInternal);
        }

        public virtual SimEventSubscription SubscribeCommand(string name, bool isInternal = false)
        {
            return Subscribe(name, EVENT_GROUP_COMMAND, isInternal);
        }

        protected virtual SimEventSubscription Subscribe(string name, MappedID groupID, bool isInternal = false)
        {
            if (Manager.Config.VerboseLogging)
                Logger.Verbose($"Create new Subscription for Event '{name}'");
            SimEvent @event;
            if (!HasName(name, out uint id))
            {
                MappedID eventId;
                if (isInternal)
                    eventId = IdStore.MapConstant(name);
                else
                    eventId = IdStore.GetNext();
                @event = new SimEvent(name, eventId, groupID, this, isInternal);
                Resources.Add(eventId, @event);
                if (Manager.Config.VerboseLogging)
                    Logger.Verbose($"Added new Event for '{name}' on Id '{eventId}'");
            }
            else
                @event = Resources[id];

            if (@event == null)
            {
                Logger.Warning($"Returned Event for '{name}' is NULL");
                return null;
            }

            return new SimEventSubscription(@event);
        }

        public override void CheckState()
        {
            
        }

        protected override void Unregister(bool disconnect)
        {
            if (disconnect && Manager.IsReceiveRunning)
            {
                Call(sc => sc.ClearNotificationGroup(EVENT_GROUP_SUBSCRIBED));
                Call(sc => sc.ClearNotificationGroup(EVENT_GROUP_COMMAND));
            }
        }

        public override void ClearUnusedResources(bool clearAll)
        {
            if (!clearAll)
            {
                var unused = Resources.Where(kv => kv.Value.IsRegistered && !kv.Value.IsSubscribed && !kv.Value.IsInternal);
                if (unused.Any())
                {
                    Logger.Debug($"Unregister unused SimEvents: {unused.Count()}");
                    foreach (var simres in unused)
                        simres.Value.Unregister(false);
                }
            }
            else
            {
                SendEvents.Clear();
                var noninternal = Resources.Where(kv => !kv.Value.IsInternal).Select(kv => kv.Key).ToList();
                Logger.Debug($"Removing all non-internal SimEvents: {noninternal.Count}");
                foreach (var key in noninternal)
                    Resources[key].Unregister(true);
                foreach (var key in noninternal)
                    Resources.Remove(key);

                IdStore.Reset();
            }
        }

        public virtual bool SendEvent(string eventName, object[] parameter)
        {
            if (HasName(eventName, out uint id))
            {
                return Resources[id].WriteValues(parameter);
            }
            else if (!SendEvents.TryGetValue(eventName, out SimEventSubscription sub))
            {
                sub = SubscribeCommand(eventName);
                sub.Resource.Register();
                SendEvents.Add(eventName, sub);
                return sub.WriteValues(parameter);
            }
            else
                return sub.WriteValues(parameter);
        }
    }
}
