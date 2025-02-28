using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ViewModels
{
    public static class ModelHelper
    {
        public static TModel SetBinding<TModel>(TModel model, string propertyName, FrameworkElement element, DependencyProperty elementProperty, IValueConverter converter = null, ValidationRule rule = null)
            where TModel : ObservableObject
        {
            var binding = new Binding(propertyName)
            {
                Source = model
            };
            
            if (converter != null)
                binding.Converter = converter;
            if (rule != null)
                binding.ValidationRules.Add(rule);

            element.SetBinding(elementProperty, binding);
            return model;
        }
    }
}
