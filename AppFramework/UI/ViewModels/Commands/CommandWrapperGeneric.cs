using CFIT.AppLogger;
using System;

namespace CFIT.AppFramework.UI.ViewModels.Commands
{
    public class CommandWrapper<T>(Action<T> execute, Func<T, bool> canExecute = null) : CommandWrapperBase()
    {
        public virtual Action<T> ActionExecute { get; } = execute;
        public virtual Func<T, bool> FuncCanExecute { get; } = canExecute;

        protected override bool CheckCanExecute(object? parameter)
        {
            try
            {
                return FuncCanExecute == null || FuncCanExecute.Invoke((T)parameter);
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
                ActionExecute?.Invoke((T)parameter);
                NotifyExecuted();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
