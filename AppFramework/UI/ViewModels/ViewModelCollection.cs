using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ViewModels
{
    public interface IViewModelCollection
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public ICollection SourceCollection { get; }
        public int Count { get; }
        public bool UpdatesAllowed { get; set; }
        public bool CheckContained { get; set; }
        public bool AllowEmpty { get; set; }
        public void NotifyCollectionChanged(NotifyCollectionChangedEventArgs e = null);
        public void Clear();
        public IEnumerator GetEnumerator();
        public bool HasBindingErrors();
        public bool HasBinding(string propertyName, out IMemberBinding binding);
        public bool HasBinding(string propertyName);
        public bool HasBindingIndex(string propertyName, int index, out IMemberBinding binding);
        public bool HasBindingIndex(string propertyName, int index);
        public void AddMemberBinding(string bindingId, IMemberBinding binding);
        public ConcurrentDictionary<string, IMemberBinding> MemberBindings { get; }
        public int BindingCount { get; }
    }

    public class ViewModelCollection<Tin, Tout> : ObservableObject, IEnumerable, INotifyCollectionChanged, IViewModelCollection
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public virtual ICollection<Tin> Source { get; }
        public virtual ICollection SourceCollection => Source as ICollection;
        public virtual int Count => Source?.Count ?? 0;
        public virtual Func<Tin, Tout> Transformator { get; protected set; }
        public virtual Func<Tin, bool> Validator { get; }

        public virtual ConcurrentDictionary<string, IMemberBinding> MemberBindings { get; } = [];
        public virtual int BindingCount => MemberBindings.Count;

        public virtual bool UpdatesAllowed { get; set; } = true;
        public virtual bool CheckContained { get; set; } = true;
        public virtual bool AllowEmpty { get; set; } = true;

        public ViewModelCollection(ICollection<Tin> source, Func<Tin, Tout> transformator, Func<Tin, bool> validator = null)
        {
            Source = source;
            Transformator = transformator;
            Validator = (validator ?? ((item) => { return true; }));
            InitializeMemberBindings();
        }

        protected virtual void InitializeMemberBindings()
        {

        }

        public virtual void SetItemTransformator(Func<Tin, Tout> transformator)
        {
            Transformator = transformator;
            NotifyCollectionChanged();
        }

        public virtual void NotifyCollectionChanged(NotifyCollectionChangedEventArgs e = null)
        {
            e ??= new(NotifyCollectionChangedAction.Reset);
            CollectionChanged?.Invoke(this, e);
        }

        public virtual Tin BuildItemFromBindings()
        {
            return default;
        }

        public virtual IMemberBinding this[string propertyName]
        {
            get
            {
                if (!MemberBindings.TryGetValue(propertyName, out IMemberBinding binding))
                    binding = null;
                return binding;
            }
        }

        public virtual bool HasBinding(string propertyName, out IMemberBinding binding)
        {
            return MemberBindings.TryGetValue(propertyName, out binding);
        }

        public virtual bool HasBinding(string propertyName)
        {
            return MemberBindings.ContainsKey(propertyName);
        }

        public virtual bool HasBindingIndex(string propertyName, int index, out IMemberBinding binding)
        {
            return MemberBindings.TryGetValue($"{propertyName}{index}", out binding);
        }

        public virtual bool HasBindingIndex(string propertyName, int index)
        {
            return MemberBindings.ContainsKey($"{propertyName}{index}");
        }

        public virtual bool HasBindingErrors()
        {
            return BindingCount < 1 || MemberBindings.Any((kv) => kv.Value.HasError());
        }

        public virtual MemberBinding<Min, string> CreateMemberNumberBinding<Min>(string propertyName, string defaulValue = null)
        {
            return CreateMemberBinding<Min, string>(propertyName, new RealInvariantConverter(defaulValue), new ValidationRuleStringNumber());
        }

        public virtual MemberBinding<Min, string> CreateMemberIntegerBinding<Min>(string propertyName, string defaulValue = null)
        {
            return CreateMemberBinding<Min, string>(propertyName, new RealInvariantConverter(defaulValue), new ValidationRuleStringInteger());
        }

        public virtual MemberBinding<Min, string> CreateMemberStringBinding<Min>(string propertyName, IValueConverter converter = null)
        {
            return CreateMemberBinding<Min, string>(propertyName, converter, new ValidationRuleString());
        }

        public virtual MemberBinding<Min, Mout> CreateMemberIndexBinding<Min, Mout>(string propertyName, int index, IValueConverter converter, ValidationRule validator = null)
        {
            var binding = new MemberIndexBinding<Min, Mout>(propertyName, index, converter, validator, (valueIn, valueOut) => this.SetPropertyValue<Min>(propertyName, valueIn));
            AddMemberBinding($"{propertyName}{index}", binding);
            return binding;
        }

        public virtual MemberBinding<Min, Mout> CreateMemberBinding<Min, Mout>(string propertyName, IValueConverter converter, ValidationRule validator = null)
        {
            var binding = new MemberBinding<Min, Mout>(propertyName, converter, validator);
            AddMemberBinding(propertyName, binding);
            return binding;
        }

        public virtual void AddMemberBinding(string bindingId, IMemberBinding binding)
        {
            MemberBindings.Add(bindingId, binding);
        }

        public virtual bool Contains(Tin item)
        {
            try { return Source?.Contains(item) == true; }
            catch { return false; }
        }

        public virtual void Add(Tin item)
        {
            try
            {
                AddSource(item);
                NotifyCollectionChanged();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void AddSource(Tin item)
        {
            Source.Add(item);
        }

        public virtual void Remove(Tin item)
        {
            try
            {
                if (RemoveSource(item))
                    NotifyCollectionChanged();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual bool RemoveSource(Tin item)
        {
            return Source.Remove(item);
        }

        public virtual void Update(Tin oldItem, Tin newItem)
        {
            try
            {
                if (UpdatesAllowed && UpdateSource(oldItem, newItem))
                    NotifyCollectionChanged();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual bool IsUpdateAllowed(Tin oldItem, Tin newItem)
        {
            return Contains(oldItem);
        }

        public virtual bool UpdateSource(Tin oldItem, Tin newItem)
        {
            try
            {
                if (IsUpdateAllowed(oldItem, newItem))
                {
                    if (RemoveSource(oldItem))
                    {
                        AddSource(newItem);
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public virtual void Clear()
        {
            Source.Clear();
            NotifyCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }

        public virtual IEnumerator GetEnumerator()
        {
            return new TransformEnumerator(Source, Transformator);
        }

        public virtual TransformEnumerator GetTransformEnumerator()
        {
            return GetEnumerator() as TransformEnumerator;
        }

        public class TransformEnumerator(ICollection<Tin> source, Func<Tin, Tout> transformator) : IEnumerator<Tout>
        {
            public virtual int Index { get; protected set; } = 0;
            protected Tout _current = default;
            public virtual Tin CurrentSource { get; protected set; } = default;
            public virtual object Current => _current;
            Tout IEnumerator<Tout>.Current => _current;
            public virtual ICollection<Tin> Source { get; } = source;
            protected virtual IEnumerator<Tin> SourceEnumerator { get; } = source.GetEnumerator();
            protected virtual Func<Tin, Tout> Transformator { get; } = transformator;

            public virtual bool MoveNext()
            {
                if (SourceEnumerator.MoveNext())
                {
                    Index++;
                    CurrentSource = SourceEnumerator.Current;
                    _current = Transformator(CurrentSource);
                    return true;
                }
                else
                    return false;
            }

            public virtual Tin GetSource(int index)
            {
                var enumerator = Source.GetEnumerator();
                int i = 0;
                while (enumerator.MoveNext())
                {
                    if (i == index)
                        return enumerator.Current;
                    i++;
                }

                return enumerator.Current;
            }

            public virtual Tout GetTransform(int index)
            {
                var enumerator = Source.GetEnumerator();
                int i = 0;
                while (enumerator.MoveNext())
                {
                    if (i == index)
                        return Transformator(enumerator.Current);
                    i++;
                }

                return Transformator(enumerator.Current);
            }

            public virtual void Reset()
            {
                Index = 0;
                SourceEnumerator.Reset();
            }

#pragma warning disable
            public virtual void Dispose()
#pragma warning restore
            {
                SourceEnumerator.Dispose();
            }
        }
    }

    public class ViewModelDictionary<K, V, Tout>(ICollection<KeyValuePair<K, V>> source, Func<KeyValuePair<K, V>, Tout> transformator, Func<KeyValuePair<K, V>, bool> validator = null)
               : ViewModelCollection<KeyValuePair<K, V>, Tout>(source, transformator, validator)
    {
        public virtual IDictionary<K, V> Dictionary => Source as IDictionary<K, V>;

        public override bool Contains(KeyValuePair<K, V> item)
        {
            try { return Dictionary?.ContainsKey(item.Key) == true; }
            catch { return false; }
        }

        public override bool IsUpdateAllowed(KeyValuePair<K, V> oldItem, KeyValuePair<K, V> newItem)
        {
            try { return oldItem.Key?.Equals(newItem.Key) == true || !Contains(newItem); }
            catch { return false; }
        }
    }
}
