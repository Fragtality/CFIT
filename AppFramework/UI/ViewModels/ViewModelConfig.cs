using CFIT.AppFramework.AppConfig;
using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppTools;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace CFIT.AppFramework.UI.ViewModels
{
    public abstract partial class ViewModelConfig<TConfig, TDefinition> : ViewModelBase<TConfig> where TConfig : AppConfigBase<TDefinition> where TDefinition : ProductDefinitionBase
    {
        public CommandWrapper SaveCommand { get; }

        public ViewModelConfig(TConfig source) : base(source)
        {
            SaveCommand = new(() =>
            {
                if (ValidateConfiguration())
                    SaveConfiguration();
            }, ValidateConfiguration);
        }

        protected virtual T GetConfigValue<T>([CallerMemberName] string propertyName = null!)
        {
            return GetSourceValue<T>(propertyName);
        }

        protected virtual void SetConfigValue<T>(T value, Func<T, ValidationContext, ValidationResult> validator = null, [CallerMemberName] string propertyName = null!)
        {
#pragma warning disable
            Action<T, T> validateConfig = (value, oldValue) =>
#pragma warning restore
            {
                if (ValidateConfiguration())
                    SaveConfiguration();
                else
                    Source.SetPropertyValue<T>(propertyName, oldValue);
            };

            SetSourceValue<T>(value, validator, validateConfig, propertyName);
        }

        protected virtual bool ValidateConfiguration()
        {
            return true;
        }

        protected virtual void SaveConfiguration()
        {
            Source.SaveConfiguration();
        }
    }
}
