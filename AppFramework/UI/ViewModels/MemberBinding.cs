using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace CFIT.AppFramework.UI.ViewModels
{
    public partial class MemberIndexBinding<Tin, Tout>(string name, int index, IValueConverter converter, ValidationRule validator = null, Action<Tin, Tout> updateAction = null)
                       : MemberBinding<Tin, Tout>(name, converter, validator, updateAction)
    {
        public virtual int Index { get; } = index;

        public static MemberBinding<Tin, Tout> CreateStringNumber(string name, int index, Action<Tin, Tout> updateAction = null, string defaultValue = null)
        {
            return new MemberIndexBinding<Tin, Tout>(name, index, new RealInvariantConverter(defaultValue), new ValidationRuleStringNumber(), updateAction);
        }

        public static MemberBinding<Tin, Tout> CreateStringInteger(string name, int index, Action<Tin, Tout> updateAction = null, string defaultValue = null)
        {
            return new MemberIndexBinding<Tin, Tout>(name, index, new RealInvariantConverter(defaultValue), new ValidationRuleStringInteger(), updateAction);
        }

        protected override Tin GetSourceProperty(object source)
        {
            if (source?.HasProperty<Tin[]>(Name, out Tin[] sourceValue) == true)
                return sourceValue[Index];
            else
                return default;
        }
    }

    public interface IMemberBinding
    {
        public string Name { get; }
        public object Source { get; }
        public IValueConverter Converter { get; }
        public object ConverterParameter { get; set; }
        public ValidationRule Validator { get; set; }
        public FrameworkElement FrameworkElement { get; }
        public Binding Binding { get; }
        public bool NoUpdate { get; }
        public DependencyProperty BindingProperty { get; }

        public void SubscribeSource(object source);
        public void SetValueIn(object sourceValue);
        public void SetValueOut(object targetValue);
        public void SetSourceValue(object source);
        public void NotifyPropertyChanged();
        public void NotifyPropertyChanged(string propertyName);
        public T ConvertFromTarget<T>();
        public bool HasError();
        public void BindElement(FrameworkElement element, ValidationRule validator = null, DependencyProperty elementProperty = null, bool noUpdate = false);
    }

    public partial class MemberBinding<Tin, Tout> : ObservableObject, IMemberBinding
    {
        public event Action<Tin, Tout> ValidValueSet;

        public virtual string Name { get; }
        public virtual object Source { get; protected set; } = null;
        public virtual IValueConverter Converter { get; }
        public virtual object ConverterParameter { get; set; } = null;
        public virtual ValidationRule Validator { get; set; }
        public virtual FrameworkElement FrameworkElement { get; protected set; }
        public virtual Binding Binding { get; protected set; }
        public virtual bool NoUpdate { get; protected set; } = false;
        public virtual DependencyProperty BindingProperty { get; protected set; }

        public MemberBinding(string name, IValueConverter converter, ValidationRule validator = null, Action<Tin, Tout> updateAction = null)
        {
            Name = name;
            Converter = converter;
            Validator = validator;
            if (updateAction != null)
                ValidValueSet += updateAction;
        }

        public static MemberBinding<Tin, Tout> CreateStringNumber(string name, Action<Tin, Tout> updateAction = null, string defaultValue = null)
        {
            return new MemberBinding<Tin, Tout>(name, new RealInvariantConverter(defaultValue), new ValidationRuleStringNumber(), updateAction);
        }

        public static MemberBinding<Tin, Tout> CreateStringInteger(string name, Action<Tin, Tout> updateAction = null, string defaultValue = null)
        {
            return new MemberBinding<Tin, Tout>(name, new RealInvariantConverter(defaultValue), new ValidationRuleStringInteger(), updateAction);
        }

        public virtual void NotifyPropertyChanged()
        {
            NotifyPropertyChanged(nameof(Value));
        }

        public virtual void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        public virtual void SubscribeSource(object source)
        {
            if (source == null || Source != null)
                return;
            Source = source;

            if (Source is ObservableObject observable)
                observable.PropertyChanged += (_, e) => OnSourceChanged(e?.PropertyName);
            else if (Source is INotifyPropertyChanged notifyInteface)
                notifyInteface.PropertyChanged += (_, e) => OnSourceChanged(e?.PropertyName);
        }

        protected virtual void OnSourceChanged(string propertyName)
        {
            if (propertyName == Name)
                SetSourceValue(Source);
        }

        protected virtual Tin GetSourceProperty(object source)
        {
            if (source?.HasProperty<Tin>(Name, out Tin sourceValue) == true)
                return sourceValue;
            else
                return default;
        }

        public virtual Tout ConvertSourceValue(Tin sourceValue)
        {
            try
            {
                if (Converter != null)
                    return (Tout)Converter.Convert(sourceValue, typeof(Tout), ConverterParameter, CultureInfo.CurrentCulture);
                else
                    return (Tout)(object)sourceValue;
            }
            catch
            {
                return default;
            }
        }

        public virtual Tout GetSourceValue(object source)
        {
            return ConvertSourceValue(GetSourceProperty(source));
        }

        public virtual void SetValueIn(object sourceValue)
        {
            SetValueIn((Tin)sourceValue);
        }

        public virtual void SetValueOut(object targetValue)
        {
            SetValueOut((Tout)targetValue);
        }

        public virtual void SetValueIn(Tin sourceValue)
        {
            _value = ConvertSourceValue(sourceValue);
            NotifyPropertyChanged();
        }

        public virtual void SetValueOut(Tout targetValue)
        {
            _value = targetValue;
            NotifyPropertyChanged();
        }

        public virtual void SetSourceValue(object source)
        {
            _value = GetSourceValue(source);
            NotifyPropertyChanged();
        }

        public virtual T ConvertFromTarget<T>()
        {
            return (T)ConvertFromTarget();
        }

        public virtual object ConvertFromTarget()
        {
            try
            {
                return Converter.ConvertBack(_value, typeof(Tin), ConverterParameter, CultureInfo.CurrentCulture);
            }
            catch { }

            return default;
        }

        public virtual bool HasError()
        {
            return FrameworkElement == null || Validation.GetHasError(FrameworkElement) || (Validator != null && Validator.Validate(_value, CultureInfo.CurrentCulture) != ValidationResult.ValidResult) || ConvertFromTarget() == null;
        }

        public virtual bool IsTyping { get; protected set; } = false;
        public virtual bool IsValid { get; protected set; } = false;

        protected Tout _value;
        public virtual Tout Value
        {
            get { return _value; }
            set
            {
                _value = value;
                NotifyPropertyChanged();

                if (!HasError())
                {
                    IsValid = true;
                    NotifyPropertyChanged(nameof(IsValid));
                    if (!IsTyping)
                        ValidValueSet?.Invoke(ConvertFromTarget<Tin>(), _value);
                }
                else if (IsValid)
                {
                    IsValid = false;
                    NotifyPropertyChanged(nameof(IsValid));
                }
            }
        }

        public virtual void BindElement(FrameworkElement element, ValidationRule validator = null, DependencyProperty elementProperty = null, bool noUpdate = false)
        {
            FrameworkElement = element;
            NoUpdate = noUpdate;
            Binding = new Binding(nameof(Value))
            {
                Source = this,
            };
            if (!NoUpdate)
                Binding.Mode = BindingMode.TwoWay;
            BindingProperty = elementProperty ?? element.GetDefaultDependency();
            
            if (validator != null)
                Validator = validator;
            if (Validator != null)
                Binding.ValidationRules.Add(Validator);

            if (FrameworkElement is TextBox textBox)
            {
                textBox.KeyUp += (sender, e) => OnTextBoxKey(sender as TextBox, e);
                textBox.LostFocus += (sender, _) => OnTextBoxFocus(sender as TextBox);
                textBox.LostKeyboardFocus += (sender, _) => OnTextBoxFocus(sender as TextBox);
            }

            if (Source != null)
                FrameworkElement.Loaded += (_, _) => SetSourceValue(Source);
            FrameworkElement.SetBinding(BindingProperty, Binding);
        }

        protected virtual void OnTextBoxKey(TextBox sender, KeyEventArgs e)
        {
            if (sender == null)
                return;

            if (Sys.IsEnter(e))
                IsTyping = false;
            else
                IsTyping = true;
            sender.UpdateBindingSource();
        }

        protected virtual void OnTextBoxFocus(TextBox sender)
        {
            if (sender == null)
                return;

            IsTyping = false;
            sender.UpdateBindingSource();
        }
    }
}
