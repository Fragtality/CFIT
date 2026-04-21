using CFIT.AppLogger;
using System;
using System.Threading.Tasks;

namespace CFIT.AppFramework.UI.ViewModels.Commands
{
    public class CommandWrapperAsync(Func<Task> execute, Func<bool> canExecute = null) : CommandWrapperBase()
    {
        public virtual Func<Task> ActionExecute { get; } = execute;
        public virtual Func<bool> FuncCanExecute { get; } = canExecute;

        protected override bool CheckCanExecute(object? parameter)
        {
            try
            {
                return FuncCanExecute == null || FuncCanExecute.Invoke();
            }
            catch
            {
                return false;
            }
        }

        protected override async Task DoExecute(object? parameter)
        {
            try
            {
                await ActionExecute?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
