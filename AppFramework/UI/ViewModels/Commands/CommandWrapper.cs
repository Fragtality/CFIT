using CFIT.AppLogger;
using System;

namespace CFIT.AppFramework.UI.ViewModels.Commands
{
    public class CommandWrapper(Action execute, Func<bool> canExecute = null) : CommandWrapperBase()
    {
        public virtual Action ActionExecute { get; } = execute;
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

        protected override void DoExecute(object? parameter)
        {
            try
            {
                ActionExecute?.Invoke();
                NotifyExecuted();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
