using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CFIT.AppFramework.UI.ViewModels
{
    public partial class ViewModelSelector<Tin, Tout> : ObservableObject
    {
        public virtual ViewModelCollection<Tin, Tout> ItemsSource { get; }
        public virtual ICollection<Tin> Source => ItemsSource.Source;
        public virtual Func<Tin, Tout> Transformator => ItemsSource.Transformator;
        public virtual Func<Tin, bool> Validator => ItemsSource.Validator;
        public virtual bool GetTransformedSelection { get; set; } = true;

        public event Action ClearInputs;
        public virtual Selector SelectorElement { get; }
        public virtual bool HasSelection => SelectorElement?.SelectedIndex >= 0;
        protected virtual UiIconLoader IconLoader { get; set; }
        public virtual BitmapImage ImageAdd { get; protected set; }
        public virtual BitmapImage ImageEdit { get; protected set; }
        public virtual BitmapImage ImageSource => IsUpdating ? ImageEdit : ImageAdd;
        public virtual CommandWrapper AddUpdateCommand { get; protected set; }
        public virtual CommandWrapper RemoveCommand { get; protected set; }
        public virtual CommandWrapper ClearCommand { get; protected set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedDisplayItem))]
        [NotifyPropertyChangedFor(nameof(HasSelection))]
        [NotifyPropertyChangedFor(nameof(IsUpdating))]
        [NotifyPropertyChangedFor(nameof(ImageSource))]
        protected Tin _SelectedItem = default;
        public virtual Tout SelectedDisplayItem => Transformator(SelectedItem);
        public virtual bool IsUpdating => ItemsSource.UpdatesAllowed && HasSelection;

        public ViewModelSelector(Selector selector, ViewModelCollection<Tin, Tout> source)
               : this(selector, source, new(Assembly.GetExecutingAssembly(), IconLoadSource.Embedded, "CFIT.AppFramework.UI.Icons."))
        {
            
        }

        public ViewModelSelector(Selector selector, ViewModelCollection<Tin, Tout> source, UiIconLoader loader)
        {
            SelectorElement = selector;
            ItemsSource = source;

            SelectorElement.ItemsSource = ItemsSource;
            SelectorElement.SelectionChanged += SelectionChanged;
            SelectorElement.AddKeyBinding(new RelayCommand(ClearSelection), Key.Escape);
            this.PropertyChanged += SelfPropertyChanged;
            SetIconLoader(loader);
        }

        public virtual void SetIconLoader(UiIconLoader loader)
        {
            IconLoader = loader;
            ImageAdd = IconLoader.LoadIcon("add");
            ImageEdit = IconLoader.LoadIcon("edit");
        }

        protected virtual void SelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedItem))
                NotifyCanExecuteChanged();
        }

        protected virtual void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectorElement != null)
            {
                try
                {
                    if (HasSelection)
                    {
                        if (GetTransformedSelection)
                            SelectedItem = ItemsSource.GetTransformEnumerator().GetSource(SelectorElement.SelectedIndex);
                        else
                            SelectedItem = (Tin)SelectorElement.SelectedValue;
                        ItemsSource.MemberBindings.ToList().ForEach((kv) => kv.Value.SetSourceValue(SelectedItem));
                    }
                    else
                        SelectedItem = default;
                }
                catch
                {
                    SelectedItem = default;
                }
            }
        }

        public virtual void SetSelectedIndex(int index)
        {
            SelectorElement.SelectedIndex = index;
        }

        public virtual void ClearSelection()
        {
            ClearInputs?.Invoke();
            SetSelectedIndex(-1);
        }

        protected virtual void NotifyCanExecuteChanged()
        {
            AddUpdateCommand?.NotifyCanExecuteChanged();
            RemoveCommand?.NotifyCanExecuteChanged();
            ClearCommand?.NotifyCanExecuteChanged();
        }

        public virtual void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        protected virtual Tin GetItem(Func<Tin> getItem)
        {
            Tin item = getItem();
            if (IsUpdating && HasSelection && item?.Equals(default) == true)
                item = SelectedItem;
            return item;
        }

        protected virtual Func<bool> CreateAddCanExecute(Func<Tin> getItem, Func<bool> canExecute = null)
        {
            Func<bool> func;
            if (canExecute != null)
            {
                if (ItemsSource.CheckContained)
                    func = () =>
                    {
                        Tin item = GetItem(getItem);
                        return canExecute() && Validator.Invoke(item) && (!ItemsSource.Contains(item) || ItemsSource.IsUpdateAllowed(SelectedItem, item));
                    };
                else
                    func = () =>
                    {
                        return canExecute() && Validator.Invoke(GetItem(getItem));
                    };
            }
            else
            {
                if (ItemsSource.CheckContained)
                    func = () =>
                    {
                        Tin item = GetItem(getItem);
                        return Validator.Invoke(item) && (!ItemsSource.Contains(item) || ItemsSource.IsUpdateAllowed(SelectedItem, item));
                    };
                else
                    func = () =>
                    {
                        return Validator.Invoke(GetItem(getItem));
                    };
            }

            return func;
        }

        public virtual CommandWrapper BindAddUpdateButton(ButtonBase button, Image image, Func<Tin> getItem = null, Func<bool> canExecute = null)
        {
            getItem ??= ItemsSource.BuildItemFromBindings;

            AddUpdateCommand = new CommandWrapper(() =>
            {
                Tin item = getItem();
                bool containedCheck = ((ItemsSource.CheckContained && !ItemsSource.Contains(item)) || !ItemsSource.CheckContained);
                if (Validator(item))
                {
                    if (IsUpdating && ItemsSource.UpdatesAllowed)
                        ItemsSource.Update(SelectedItem, item);
                    else if (containedCheck && ItemsSource.AddAllowed)
                        ItemsSource.Add(item);    
                    ClearSelection();
                }
            }, CreateAddCanExecute(getItem, canExecute));

            AddUpdateCommand.Subscribe(SelectorElement);

            button.Command = AddUpdateCommand;
            if (image != null && ItemsSource.UpdatesAllowed)
            {
                var binding = new Binding(nameof(ImageSource)) { Source = this };
                image.SetBinding(Image.SourceProperty, binding);
            }

            return AddUpdateCommand;
        }

        public virtual CommandWrapper BindRemoveButton(ButtonBase button, Func<bool> canExecute = null)
        {
            Func<bool> func;
            if (canExecute != null)
            {
                if (ItemsSource.AllowEmpty)
                    func = () => canExecute() && HasSelection;
                else
                    func = () => canExecute() && HasSelection && ItemsSource.Count > 1;
            }
            else
            {
                if (ItemsSource.AllowEmpty)
                    func = () => HasSelection;
                else
                    func = () => HasSelection && ItemsSource.Count > 1;
            }

            RemoveCommand = new CommandWrapper(() =>
            {
                if (func())
                {
                    ItemsSource.Remove(SelectedItem);
                    SelectorElement.SelectedIndex = -1;
                    ClearInputs?.Invoke();
                    NotifyCanExecuteChanged();
                }
            }, func);

            RemoveCommand.Subscribe(SelectorElement);
            button.Command = RemoveCommand;
            return RemoveCommand;
        }

        public virtual CommandWrapper BindClearButton(ButtonBase button)
        {
            bool canExecute() => ((!ItemsSource.AllowEmpty && ItemsSource.Count > 1) || (ItemsSource.AllowEmpty && ItemsSource.Count > 0));
            ClearCommand = new(ItemsSource.Clear, canExecute);

            button.Command = ClearCommand;
            return ClearCommand;
        }

        protected virtual void OnSelectionChangedBinding(IMemberBinding binding)
        {
            binding.SetSourceValue(SelectedItem);

            if (binding.NoUpdate && IsUpdating)
                binding.FrameworkElement.IsEnabled = false;
            else
                binding.FrameworkElement.IsEnabled = true;
        }

        public virtual void BindMemberIndex(FrameworkElement frameworkElement, string propertyName, int index, DependencyProperty dependencyProperty = null, object defaultValue = null, bool mapAddEnter = false, bool noUpdate = false)
        {
            BindMember(frameworkElement, $"{propertyName}{index}", dependencyProperty, defaultValue, mapAddEnter, noUpdate);
        }

        public virtual void BindMember(FrameworkElement frameworkElement, string propertyName, DependencyProperty dependencyProperty = null, object defaultValue = null, bool mapAddEnter = false, bool noUpdate = false)
        {
            if (!ItemsSource.HasBinding(propertyName, out IMemberBinding binding))
                return;
            binding.BindElement(frameworkElement, null, dependencyProperty, noUpdate);

            if (frameworkElement is TextBox textBox)
            {
                this.ClearInputs += () => binding.SetValueOut(defaultValue?.ToString() ?? "");
                if (mapAddEnter && AddUpdateCommand != null)
                    AddUpdateCommand.Bind(textBox);
            }
            else if (frameworkElement is Label label && dependencyProperty == Label.ContentProperty)
                this.ClearInputs += () => binding.SetValueOut(defaultValue?.ToString() ?? "");
            else if (frameworkElement is TextBlock textBlock)
                this.ClearInputs += () => binding.SetValueOut(defaultValue?.ToString() ?? "");
            else if (defaultValue != null)
                this.ClearInputs += () => binding.SetValueOut(defaultValue);

                SubscribeElement(frameworkElement);
        }

        public virtual void BindTextElement(FrameworkElement frameworkElement, string memberPath = null, string defaultValue = "", IValueConverter converter = null, bool mapAddEnter = false, bool noUpdate = false)
        {
            SelectorElement.SelectionChanged += (sender, e) =>
            {
                object value;
                if (memberPath == null)
                    value = SelectedItem;
                else
                    value = SelectedItem?.GetPropertyValue<object>(memberPath);

                if (converter != null)
                    value = converter?.Convert(value, typeof(string), null, CultureInfo.CurrentUICulture);

                if (frameworkElement is Label label)
                    label.Content = value?.ToString() ?? defaultValue;
                else if (frameworkElement is TextBlock textBlock)
                    textBlock.Text = value?.ToString() ?? defaultValue;
                else if (frameworkElement is TextBox textBox)
                    textBox.Text = value?.ToString() ?? defaultValue;

                if (noUpdate && IsUpdating)
                    frameworkElement.IsEnabled = false;
                else
                    frameworkElement.IsEnabled = true;
            };

            if (frameworkElement is TextBox textBox)
            {
                this.ClearInputs += () => textBox.Text = defaultValue;
                if (mapAddEnter && AddUpdateCommand != null)
                    AddUpdateCommand.Bind(textBox);
            }
            else if (frameworkElement is Label label)
                this.ClearInputs += () => label.Content = defaultValue;
            else if (frameworkElement is TextBlock textBlock)
                this.ClearInputs += () => textBlock.Text = defaultValue;

            SubscribeElement(frameworkElement);
        }

        public virtual void SubscribeElement(FrameworkElement frameworkElement)
        {
            AddUpdateCommand?.Subscribe(frameworkElement);
            RemoveCommand?.Subscribe(frameworkElement);
            ClearCommand?.Subscribe(frameworkElement);
        }
    }
}
