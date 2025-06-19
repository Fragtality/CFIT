using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.Modules;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CFIT.SimConnectLib.SimResources
{
    public interface ISimResourceManager
    {
        public bool GetResource(uint id, out ISimResource resource);
    }

    public abstract class SimResourceManager<TManager, TResource, TSubscription> : SimConnectModule, ISimResourceManager
        where TManager : SimResourceManager<TManager, TResource, TSubscription>
        where TResource : SimResource<TManager, TResource, TSubscription>
        where TSubscription : SimResourceSubscription<TManager, TResource, TSubscription>
    {
        protected virtual MappedIdStore IdStore { get; }
        public virtual ConcurrentDictionary<uint, TResource> Resources { get; } = [];
        protected virtual ConcurrentDictionary<uint, bool> RegisteredDataDefinitions { get; } = [];

        public SimResourceManager(SimConnectManager manager, object moduleParams) : base(manager, moduleParams)
        {
            IdStore = AllocateStore();
        }

        protected override void SetModuleParams(object moduleParams)
        {
            
        }

        protected abstract MappedIdStore AllocateStore();

        public virtual bool HasName(string name)
        {
            return HasName(name, out _);
        }

        public virtual bool HasName(string name, out uint id)
        {
            var query = Resources.Where(kv => kv.Value.Name == name);
            if (query.Any())
            {
                id = query.First().Key;
                return true;
            }
            else
            {
                id = MappedID.Default();
                return false;
            }
        }

        public virtual TResource GetResource(uint id)
        {
            if (Resources.TryGetValue(id, out TResource resource))
                return resource;
            else
                return null;
        }

        public virtual bool GetResource(uint id, out TResource resource)
        {
            resource = GetResource(id);
            return resource != null;
        }

        public virtual bool GetResource(uint id, out ISimResource resource)
        {
            resource = GetResource(id);
            return resource != null;
        }

        public abstract TSubscription Subscribe(string name, bool isInternal);

        public virtual void Unsubscribe(TSubscription subscription)
        {
            if (!GetResource(subscription.Resource.Id, out TResource resource))
                return;

            resource.Unsubscribe(subscription);
        }

        public override async Task<int> CheckResources()
        {
            var querySub = Resources.Where(kv => !kv.Value.IsRegistered && kv.Value.IsSubscribed);
            if (querySub.Any())
                Logger.Debug($"Subscribing {querySub.Count()} Resources");
            foreach (var kv in querySub)
                await kv.Value.Register();

            return querySub.Count();
        }

        public override async Task UnregisterModule(bool disconnect)
        {
            foreach (var resource in Resources)
                await resource.Value.Unregister(disconnect);

            await Unregister(disconnect);
        }

        protected abstract Task Unregister(bool disconnect);

        public virtual bool AddDataDefinition(MappedID id)
        {
            if (!RegisteredDataDefinitions.ContainsKey(id))
            {
                RegisteredDataDefinitions.Add(id);
                return true;
            }
            else
                return false;            
        }

        public virtual bool ClearDataDefinition(MappedID id)
        {
            if (RegisteredDataDefinitions.ContainsKey(id))
            {
                RegisteredDataDefinitions.Remove(id);
                return true;
            }
            else
                return false;
        }
    }
}
