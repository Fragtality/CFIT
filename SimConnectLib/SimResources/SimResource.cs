using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.SimConnectLib.SimResources
{
    public interface ISimResource
    {
        public ISimResourceManager ResourceManager { get; }
        public string Name { get; }
        public bool IsInternal { get; }
        public MappedID Id { get; }
        public object ValueStore { get; }
        public bool IsNumeric { get; }
        public bool IsString { get; }
        public bool IsStruct { get; }

        public event Action OnReceived;
        public int Subscriptions { get; }
        public bool IsSubscribed { get; }
        public bool IsRegistered { get; }
        public bool IsReceived { get;}
        public Task Register();

        public Task Request();

        public Task Unregister(bool disconnect);
        public string ToString();
        public T GetValue<T>();
        public double GetNumber();
        public string GetString();
        public void SetValue(object value);
        public void SetValues(object[] values);
        public Task<bool> WriteValue(object value);
        public Task<bool> WriteValues(object[] values);
    }

    public abstract class SimResource<TManager, TResource, TSubscription>(string name, MappedID id, TManager manager, bool isInternal) : ISimResource
        where TManager : SimResourceManager<TManager, TResource, TSubscription>
        where TResource : SimResource<TManager, TResource, TSubscription>
        where TSubscription : SimResourceSubscription<TManager, TResource, TSubscription>
    {
        public virtual TManager Manager { get; } = manager;
        public virtual ISimResourceManager ResourceManager { get { return Manager; } }
        public virtual string Name { get; } = name;
        public virtual bool IsInternal { get; } = isInternal;
        public virtual object ValueStore { get; protected set; }
        protected SemaphoreSlim _lock = new(1, 1);
        public abstract bool IsNumeric { get; }
        public abstract bool IsString { get; }
        public abstract bool IsStruct { get; }
        public virtual MappedID Id { get; } = id;
        public event Action OnReceived;
        public virtual int Subscriptions { get { return OnReceived?.GetInvocationList()?.Length ?? 0; } }
        public virtual bool IsSubscribed { get { return Subscriptions > 0; } }
        public virtual bool IsRegistered { get; protected set; } = false;
        public virtual bool IsReceived { get; protected set; } = false;

        public abstract Task Register();

        public abstract Task Request();

        public abstract Task Unregister(bool disconnect);

        protected virtual async Task<bool> Call(Action<SimConnect> action)
        {
            return await Manager?.Call(action);
        }

        public virtual T GetValue<T>()
        {
            try
            {
                if (ValueStore == null)
                    return default;
                else if (IsNumeric)
                {
                    return NumberConverter<T>();
                }
                else if (IsString)
                {
                    return StringConverter<T>();
                }
                else if (IsStruct)
                {
                    return StructConverter<T>();
                }
                else
                    return default;

            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return default;
            }
        }

        protected virtual T NumberConverter<T>()
        {
            if (typeof(T) == typeof(double))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(uint))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(float))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(int))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(long))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(bool))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(ulong))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(ushort))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(short))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(byte))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(sbyte))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(string))
                return (T)Convert.ChangeType(ValueStore.ToString(), typeof(string));
            else
                return default;
        }

        protected virtual T StringConverter<T>()
        {
            if (typeof(T) == typeof(string))
                return (T)Convert.ChangeType(ValueStore, typeof(T));
            else if (typeof(T) == typeof(double) && Conversion.IsNumber((string)ValueStore, out double @double))
                return (T)Convert.ChangeType(@double, typeof(T));
            else if (typeof(T) == typeof(uint) && Conversion.IsNumberI((string)ValueStore, out int @uint))
                return (T)Convert.ChangeType(@uint, typeof(T));
            else if (typeof(T) == typeof(float) && Conversion.IsNumberF((string)ValueStore, out float @float))
                return (T)Convert.ChangeType(@float, typeof(T));
            else if (typeof(T) == typeof(int) && Conversion.IsNumberI((string)ValueStore, out int @int))
                return (T)Convert.ChangeType(@int, typeof(T));
            else if (typeof(T) == typeof(long) && Conversion.IsNumberI((string)ValueStore, out int @long))
                return (T)Convert.ChangeType(@long, typeof(T));
            else if (typeof(T) == typeof(bool) && Conversion.IsNumberI((string)ValueStore, out int @bool))
                return (T)Convert.ChangeType(@bool, typeof(T));
            else if (typeof(T) == typeof(ulong) && Conversion.IsNumberI((string)ValueStore, out int @ulong))
                return (T)Convert.ChangeType(@ulong, typeof(T));
            else if (typeof(T) == typeof(ushort) && Conversion.IsNumberI((string)ValueStore, out int @ushort))
                return (T)Convert.ChangeType(@ushort, typeof(T));
            else if (typeof(T) == typeof(short) && Conversion.IsNumberI((string)ValueStore, out int @short))
                return (T)Convert.ChangeType(@short, typeof(T));
            else if (typeof(T) == typeof(byte) && Conversion.IsNumberI((string)ValueStore, out int @byte))
                return (T)Convert.ChangeType(@byte, typeof(T));
            else if (typeof(T) == typeof(sbyte) && Conversion.IsNumberI((string)ValueStore, out int @sbyte))
                return (T)Convert.ChangeType(@sbyte, typeof(T));
            else
                return default;
        }

        protected virtual T StructConverter<T>()
        {
            return default;
        }

        public virtual double GetNumber()
        {
            return GetValue<double>();
        }

        public virtual string GetString()
        {
            return GetValue<string>();
        }

        protected virtual bool SetStore(object value)
        {
            ValueStore = value;
            return true;
        }

        public virtual void SetValue(object value)
        {
            lock (_lock)
            {
                try
                {
                    if (!SetStore(value))
                        return;
                    SetReceived();
                    NotifySubscribers();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        public virtual void SetValues(object[] values)
        {
            if (values?.Length >= 1)
                SetValue(values[0]);
            else
                Logger.Warning($"Received Array was null or empty");
        }

        public abstract Task<bool> WriteValue(object value);

        public virtual async Task<bool> WriteValues(object[] values)
        {
            if (values?.Length >= 1)
                return await WriteValue(values[0]);
            else
                Logger.Warning($"Received Array was null or empty");
            return false;
        }

        protected virtual void SetReceived()
        {
            if (!IsReceived)
            {
                if (Manager.Manager.Config.VerboseLogging)
                    Logger.Verbose($"Value received for {this.GetType().Name} '{Name}'");
                IsReceived = true;
            }
        }

        protected virtual void NotifySubscribers()
        {
            if (!IsSubscribed)
                return;

            try
            {
                OnReceived?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual void Subscribe(SimResourceSubscription<TManager, TResource, TSubscription> subscription)
        {
            OnReceived += subscription.Update;
        }

        public virtual void Unsubscribe(SimResourceSubscription<TManager, TResource, TSubscription> subscription)
        {
            OnReceived -= subscription.Update;
            if (!IsSubscribed && !IsInternal)
                Unregister(false);
        }

        public abstract override string ToString();
    }
}
