using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace CFIT.AppFramework.UI.ViewModels
{
    public static class ModelExtensions
    {
        public static void SubscribeProperty(this INotifyPropertyChanged @interface, string propertyName, Action action)
        {
            @interface.PropertyChanged += (_, e) =>
            {
                if (e?.PropertyName == propertyName)
                    action?.Invoke();
            };
        }

        public static void SubscribeProperty(this ObservableObject @object, string propertyName, Action action)
        {
            @object.PropertyChanged += (_, e) =>
            {
                if (e?.PropertyName == propertyName)
                    action?.Invoke();
            };
        }

        public static DependencyProperty GetDefaultDependency(this FrameworkElement element)
        {
            if (element is CheckBox)
                return CheckBox.IsCheckedProperty;
            else if (element is RadioButton)
                return RadioButton.IsCheckedProperty;
            else if (element is TextBlock)
                return TextBlock.TextProperty;
            else if (element is TextBox)
                return TextBox.TextProperty;
            else if (element is Label)
                return Label.ContentProperty;
            else if (element is Image)
                return Image.SourceProperty;
            else if (element is Selector)
                return Selector.SelectedValueProperty;
            else
                return null;
        }

        public static void SetComboBox<T>(this ComboBox comboBox, Dictionary<T, string> dict, T selected)
        {
            comboBox.SelectedValuePath = "Key";
            comboBox.DisplayMemberPath = "Value";
            comboBox.ItemsSource = dict.ToList();

            int index = 0;
            foreach (T type in dict.Keys)
            {
                if (EqualityComparer<T>.Default.Equals(type, selected))
                    comboBox.SelectedIndex = index;
                index++;
            }
        }

        public static TextBoxBase AddKeyBinding(this TextBoxBase textBox, ICommand command, Key key, ModifierKeys modifiers = ModifierKeys.None)
        {
            textBox.InputBindings.Add(new KeyBinding(command, key, modifiers));
            return textBox;
        }

        public static FrameworkElement AddKeyBinding(this FrameworkElement frameworkElement, ICommand command, Key key, ModifierKeys modifiers = ModifierKeys.None)
        {
            frameworkElement.InputBindings.Add(new KeyBinding(command, key, modifiers));
            return frameworkElement;
        }

        public static Binding SetBinding(string propertyName, FrameworkElement element, DependencyProperty elementProperty, IValueConverter converter = null)
        {
            var binding = new Binding(propertyName);
            if (converter != null)
                binding.Converter = converter;

            element.SetBinding(elementProperty, binding);
            return binding;
        }

        public static void BindModelProperty<T>(this Selector selector, ObservableObject observableObject, string propertyName, Func<T> getDefault, string memberPath = null, IValueConverter converter = null)
        {
            selector.SelectionChanged += (sender, e) =>
            {
                object value;
                if (memberPath == null)
                    value = (sender as Selector)?.SelectedValue;
                else
                    value = (sender as Selector)?.SelectedValue?.GetPropertyValue<object>(memberPath);

                if (converter != null)
                    value = converter?.Convert(value, typeof(T), null, CultureInfo.CurrentUICulture);

                if (value != null)
                    observableObject?.SetPropertyValue<T>(propertyName, (T)value);
                else
                    observableObject?.SetPropertyValue<T>(propertyName, getDefault());
            };
        }

        public static void BindTextElement(this Selector selector, FrameworkElement element, string memberPath = null, IValueConverter converter = null)
        {
            selector.SelectionChanged += (sender, e) =>
            {
                object value;
                if (memberPath == null)
                    value = (sender as Selector)?.SelectedValue;
                else
                    value = (sender as Selector)?.SelectedValue?.GetPropertyValue<object>(memberPath);

                if (converter != null)
                    value = converter?.Convert(value, typeof(string), null, CultureInfo.CurrentUICulture);

                if (element is Label label)
                    label.Content = value?.ToString();
                else if (element is TextBlock textBlock)
                    textBlock.Text = value?.ToString();
                else if (element is TextBox textBox)
                    textBox.Text = value?.ToString();
            };
        }

        public static void UpdateBindingOnEnter(this TextBox textBox)
        {
            textBox.KeyUp += (sender, e) => {
                if (Sys.IsEnter(e))
                    UpdateBindingTextSource(sender);
            };
        }

        public static void UpdateBindingOnLostFocus(this TextBox textBox)
        {
            textBox.LostFocus += (sender, _) => UpdateBindingTextSource(sender);
            textBox.LostKeyboardFocus += (sender, _) => UpdateBindingTextSource(sender);
        }

        public static void UpdateBindingTextSource(object textBox)
        {
            (textBox as TextBox)?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }

        public static void UpdateBindingTarget(this FrameworkElement element, DependencyProperty dependencyProperty = null)
        {
            if (element == null)
                return;

            if (dependencyProperty == null)
            {
                if (element is TextBox)
                    dependencyProperty = TextBox.TextProperty;
                else if (element is TextBlock)
                    dependencyProperty = TextBlock.TextProperty;
                else if (element is Label)
                    dependencyProperty = Label.ContentProperty;
                else if (element is CheckBox)
                    dependencyProperty = CheckBox.IsCheckedProperty;
                else if (element is RadioButton)
                    dependencyProperty = RadioButton.IsCheckedProperty;
                else if (element is Button)
                    dependencyProperty = Button.IsEnabledProperty;
                else if (element is Image)
                    dependencyProperty = Image.SourceProperty;
                else if (element is Selector)
                    dependencyProperty = Selector.ItemsSourceProperty;
            }

            if (dependencyProperty != null)
                element.GetBindingExpression(dependencyProperty).UpdateTarget();
        }

        public static void UpdateBindingSource(this FrameworkElement element, DependencyProperty dependencyProperty = null)
        {
            if (element == null)
                return;

            if (dependencyProperty == null)
            {
                if (element is TextBox)
                    dependencyProperty = TextBox.TextProperty;
                else if (element is TextBlock)
                    dependencyProperty = TextBlock.TextProperty;
                else if (element is Label)
                    dependencyProperty = Label.ContentProperty;
                else if (element is CheckBox)
                    dependencyProperty = CheckBox.IsCheckedProperty;
                else if (element is RadioButton)
                    dependencyProperty = RadioButton.IsCheckedProperty;
                else if (element is Button)
                    dependencyProperty = Button.IsEnabledProperty;
                else if (element is Image)
                    dependencyProperty = Image.SourceProperty;
                else if (element is Selector)
                    dependencyProperty = Selector.ItemsSourceProperty;
            }

            if (dependencyProperty != null)
                element.GetBindingExpression(dependencyProperty).UpdateSource();
        }
    }
}
