using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ViewModels
{
    public abstract partial class ViewModelBase<TObject> : ObservableValidator where TObject : class
    {
        public virtual TObject Source { get; }
        public virtual bool UseRealInvariant { get; set; } = true;
        public virtual ConcurrentDictionary<string, IMemberBinding> MemberBindings { get; } = [];
        public virtual int BindingCount => MemberBindings.Count;

        public event Action<TObject> ModelUpdated;
        public ViewModelBase(TObject source)
        {
            Source = source;
            InitializeModel();
            InitializeMemberBindings();
        }

        protected virtual void InitializeMemberBindings()
        {

        }

        protected abstract void InitializeModel();

        public virtual void NotifyModelUpdated()
        {
            ModelUpdated?.Invoke(Source);
        }

        public virtual T GetSourceValue<T>([CallerMemberName] string propertyName = null!)
        {
            if (Source.IsPropertyType<T>(propertyName))
                return Source.GetPropertyValue<T>(propertyName);
            else
                return default;
        }

        public virtual void SetSourceValue<T>(T value, Func<T, System.ComponentModel.DataAnnotations.ValidationContext, System.ComponentModel.DataAnnotations.ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!)
        {
            if (!Source.IsPropertyType<T>(propertyName))
                return;

            OnPropertyChanging(propertyName);
            if (validator == null || validator.Invoke(value, null) == System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                T oldValue = GetSourceValue<T>(propertyName);
                Source.SetPropertyValue<T>(propertyName, value);

                callback?.Invoke(value, oldValue);

                OnPropertyChanged(propertyName);
            }
            else
                OnPropertyChanged(propertyName);
        }

        protected virtual void SetSourceArray<T>(T value, int index, Func<T, System.ComponentModel.DataAnnotations.ValidationContext, System.ComponentModel.DataAnnotations.ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!)
        {
            if (!Source.IsPropertyType<T[]>(propertyName))
                return;

            OnPropertyChanging(propertyName);
            T[] values = GetSourceValue<T[]>(propertyName);
            if (index >= 0 && index < values.Length && (validator == null || validator.Invoke(value, null) == System.ComponentModel.DataAnnotations.ValidationResult.Success))
            {
                T oldValue = values[index];
                values[index] = value;

                callback?.Invoke(value, oldValue);

                OnPropertyChanged(propertyName);
            }
            else
                OnPropertyChanged(propertyName);
        }

        protected virtual void StepProperty<T>(double step = 1, T[] range = default, [CallerMemberName] string propertyName = null!)
        {
            T value = GetSourceValue<T>(propertyName);
            value = Extensions.StepNumber<T>(value, step);

            if (range?.Length != 2 || (range?.Length == 2 && Extensions.CompareRange<T>(value, range)))
                SetSourceValue<T>(value, null, null, propertyName);
        }

        public virtual void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        public virtual void SubscribeProperty(string propertyName, Action callback)
        {
            this.PropertyChanged += (sender, e) => { PropertyChangedAction(e.PropertyName, propertyName, callback); };
        }

        protected virtual void PropertyChangedAction(string updatedProperty, string propertyName, Action callback)
        {
            if (updatedProperty == propertyName)
                callback?.Invoke();
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

        public virtual MemberBinding<Tin, string> CreateMemberNumberBinding<Tin>(string propertyName, string defaulValue = null)
        {
            return CreateMemberBinding<Tin, string>(propertyName, new RealInvariantConverter(defaulValue), new ValidationRuleStringNumber());
        }

        public virtual MemberBinding<Tin, string> CreateMemberIntegerBinding<Tin>(string propertyName, string defaulValue = null)
        {
            return CreateMemberBinding<Tin, string>(propertyName, new RealInvariantConverter(defaulValue), new ValidationRuleStringInteger());
        }

        public virtual MemberBinding<Tin, string> CreateMemberStringBinding<Tin>(string propertyName, IValueConverter converter = null)
        {
            return CreateMemberBinding<Tin, string>(propertyName, converter, new ValidationRuleString());
        }

        public virtual MemberBinding<Tin, Tout> CreateMemberIndexBinding<Tin, Tout>(string propertyName, int index, IValueConverter converter, ValidationRule validator = null)
        {
            var binding = new MemberIndexBinding<Tin, Tout>(propertyName, index, converter, validator, (valueIn, valueOut) => this.SetPropertyValue<Tin>(propertyName, valueIn));
            AddMemberBinding($"{propertyName}{index}", binding);
            return binding;
        }

        public virtual MemberBinding<Tin, Tout> CreateMemberBinding<Tin, Tout>(string propertyName, IValueConverter converter, ValidationRule validator = null)
        {
            var binding = new MemberBinding<Tin, Tout>(propertyName, converter, validator, (valueIn, valueOut) => this.SetPropertyValue<Tin>(propertyName, valueIn));
            AddMemberBinding(propertyName, binding);
            return binding;
        }

        public virtual void AddMemberBinding(string bindingId, IMemberBinding binding)
        {
            binding.SubscribeSource(this);
            MemberBindings.Add(bindingId, binding);
        }

        public virtual void BindSelector<T>(Selector selector, string propertyName, bool useValue = true)
        {
            selector.SelectionChanged += (sender, e) =>
            {
                if (sender is not Selector selector)
                    return;
                this.SetPropertyValue<T>(propertyName, useValue ? (T)selector.SelectedValue : (T)selector.SelectedItem);
            };
        }

        public virtual void BindStringNumber(string propertyName, FrameworkElement frameworkElement, string defaultValue = null, ValidationRule rule = null)
        {
            BindElement(propertyName, frameworkElement, new RealInvariantConverter(defaultValue), rule ?? new ValidationRuleStringNumber());
        }

        public virtual void BindStringInteger(string propertyName, FrameworkElement frameworkElement, string defaultValue = null, ValidationRule rule = null)
        {
            BindElement(propertyName, frameworkElement, new RealInvariantConverter(defaultValue), rule ?? new ValidationRuleStringInteger());
        }

        public virtual void BindElement(string propertyName, FrameworkElement element, IValueConverter converter = null, ValidationRule rule = null)
        {
            if (!this.HasProperty(propertyName) || element == null)
                return;

            var elementProperty = element?.GetDefaultDependency();
            if (elementProperty == null)
                return;

            if (element is not TextBox textBox)
                ModelHelper.SetBinding<ViewModelBase<TObject>>(this, propertyName, element, elementProperty, converter, rule);
            else
                BindTextBox(propertyName, textBox, converter, rule);
        }

        protected virtual void BindTextBox(string propertyName, TextBox textBox, IValueConverter converter = null, ValidationRule rule = null)
        {
            textBox.UpdateBindingOnEnter();
            textBox.UpdateBindingOnLostFocus();

            ModelHelper.SetBinding<ViewModelBase<TObject>>(this, propertyName, textBox, TextBox.TextProperty, CheckConverter(propertyName, converter), rule);
        }

        protected IValueConverter CheckConverter(string propertyName, IValueConverter converter)
        {
            if (converter != null)
                return converter;

            if (UseRealInvariant && (this.IsPropertyType<float>(propertyName) || this.IsPropertyType<double>(propertyName)))
                return new RealInvariantConverter();

            return null;
        }
    }
}
