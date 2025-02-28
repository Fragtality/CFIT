using CFIT.AppLogger;
using CFIT.SimConnectLib.Definitions;
using System;
using System.Threading.Tasks;

namespace CFIT.SimConnectLib.SimResources
{
    public interface ISimResourceSubscription
    {
        public ISimResource SimResource { get; }
        public string Name { get; }
        public bool IsInternal { get; }
        public MappedID Id { get; }
        public event Action<ISimResourceSubscription, object> OnReceived;
        public bool IsChanged { get; }
        public bool IsActive { get; }
        public void Subscribe();
        public void Unsubscribe();
        public T GetValue<T>();
        public double GetNumber();
        public string GetString();
        public bool WriteValue(object value);
        public bool WriteValues(object[] values);
        public string ToString();
    }

    public abstract class SimResourceSubscription<TManager, TResource, TSubscription> : IDisposable, ISimResourceSubscription
        where TManager : SimResourceManager<TManager, TResource, TSubscription>
        where TResource : SimResource<TManager, TResource, TSubscription>
        where TSubscription : SimResourceSubscription<TManager, TResource, TSubscription>
    {
        public virtual TResource Resource { get; }
        public virtual ISimResource SimResource { get { return Resource; } }
        public virtual string Name { get { return Resource.Name; } }
        public virtual bool IsInternal { get { return Resource.IsInternal; } }
        public virtual MappedID Id { get { return Resource.Id; } }
        protected virtual TManager Manager { get { return Resource.Manager; } }
        public virtual bool IsChanged { get; protected set; }
        public event Action<ISimResourceSubscription, object> OnReceived;
        public virtual bool ResetOnRead { get; set; } = true;
        public virtual bool ResetOnCallback { get; set; } = true;
        protected virtual object LastValue { get; set; }
        protected virtual bool IsSubscribed { get; set; } = true;
        public virtual bool IsActive { get { return IsSubscribed && Resource?.IsRegistered == true; } }
        protected virtual bool IsDisposed { get; set; } = false;

        public SimResourceSubscription(TResource resource)
        {
            Resource = resource;
            Resource.Subscribe(this);
        }

        public virtual T GetValue<T>()
        {
            if (ResetOnRead)
                ResetChanged();
            return Resource.GetValue<T>();
        }

        public virtual double GetNumber()
        {
            return GetValue<double>();
        }

        public virtual string GetString()
        {
            return GetValue<string>();
        }

        public virtual object PeekValue()
        {
            return Resource.ValueStore;
        }

        public virtual void Update()
        {
            if (ChangeCondition())
            {
                if (Manager.Manager.Config.VerboseLogging)
                    Logger.Verbose($"Value for {this.GetType().Name} '{Resource.Name}' changed/received");
                IsChanged = true;
                Callback();
            }
            SetLastValue();
        }

        protected virtual bool ChangeCondition()
        {
            return !IsChanged && !CompareEqual();
        }

        protected virtual bool CompareEqual()
        {
            return Resource?.ValueStore?.Equals(LastValue) == true;
        }

        protected abstract bool BlockCallback();

        protected virtual void Callback()
        {
            if (OnReceived?.GetInvocationList()?.Length == 0)
                return;

            if (BlockCallback())
                return;

            try
            {
                if (Manager.Manager.Config.VerboseLogging)
                    Logger.Verbose($"Executing Callback for '{Resource.Name}'");
                _ = Task.Run(() => { OnReceived?.Invoke(this, Resource.ValueStore); });
                if (ResetOnCallback)
                    ResetChanged();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual void ResetChanged()
        {
            IsChanged = false;
        }

        protected virtual void SetLastValue()
        {
            try
            {
                LastValue = Resource.ValueStore;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual void Subscribe()
        {
            if (Resource == null)
            {
                Logger.Warning($"Subscribe on null Resource");
                return;
            }

            if (!IsSubscribed)
                Resource.Subscribe(this);

            if (!Resource.IsRegistered)
                Resource.Register();
        }

        public virtual void Unsubscribe()
        {
            IsSubscribed = false;
            Manager.Unsubscribe(this as TSubscription);
        }

        public virtual bool WriteValue(object value)
        {
            return Resource.WriteValue(value);
        }

        public virtual bool WriteValues(object[] values)
        {
            return Resource.WriteValues(values);
        }

        public override string ToString()
        {
            return $"{Resource?.Name} = {Resource}";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (IsSubscribed)
                        Unsubscribe();
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
