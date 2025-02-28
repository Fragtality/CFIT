using CFIT.AppLogger;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace CFIT.AppFramework.UI.ViewModels.Commands
{
    public interface ICommandWrapper : ICommand
    {
        public event Action Executed;

        public ICommandWrapper Bind(object element, MouseAction mouseAction = MouseAction.LeftClick);
        public ICommandWrapper Subscribe(object @object, string propertyName);
        public ICommandWrapper Subscribe(object element);
        public void NotifyCanExecuteChanged();
    }

    public abstract class CommandWrapperBase() : ICommandWrapper
    {
        public event EventHandler? CanExecuteChanged;
        public event Action Executed;

        public virtual ICommandWrapper Bind(object element, MouseAction mouseAction = MouseAction.LeftClick)
        {
            if (element is ButtonBase button)
                button.Command = this;
            else if (element is TextBoxBase textBox)
            {
                textBox.AddKeyBinding(this, Key.Enter);
                textBox.AddKeyBinding(this, Key.Return);
            }
            else if (element is UIElement uIElement)
                uIElement.InputBindings.Add(new MouseBinding(this, new MouseGesture(mouseAction, ModifierKeys.None)));

            return this;
        }

        public virtual ICommandWrapper Subscribe(object @object, string propertyName)
        {
            if (@object is INotifyPropertyChanged @interface)
                @interface.SubscribeProperty(propertyName, NotifyCanExecuteChanged);
            else if (@object is ObservableObject observable)
                observable.SubscribeProperty(propertyName, NotifyCanExecuteChanged);

            return this;
        }

        public virtual ICommandWrapper Subscribe(object element)
        {
            if (element is ButtonBase button)
                button.Click += (_, _) => NotifyCanExecuteChanged();
            else if (element is Selector selector)
                selector.SelectionChanged += (_, _) => NotifyCanExecuteChanged();
            else if (element is TextBoxBase textBox)
                textBox.KeyUp += (_, _) => NotifyCanExecuteChanged();
            else if (element is UIElement uIElement)
                uIElement.MouseLeftButtonUp += (_, _) => NotifyCanExecuteChanged();
            else if (element is ObservableObject observableObject)
                observableObject.PropertyChanged += (_, _) => NotifyCanExecuteChanged();
            else if (element is INotifyPropertyChanged notifyInterface)
                notifyInterface.PropertyChanged += (_, _) => NotifyCanExecuteChanged();
            else if (element is INotifyCollectionChanged collectionChanged)
                collectionChanged.CollectionChanged += (_, _) => NotifyCanExecuteChanged();

            return this;
        }

        public virtual bool CanExecute(object? parameter)
        {
            try
            {
                return CheckCanExecute(parameter);
            }
            catch
            {
                return false;
            }
        }

        protected abstract bool CheckCanExecute(object? parameter);

        public virtual void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public virtual void NotifyExecuted()
        {
            Executed?.Invoke();
        }

        public virtual void Execute(object? parameter)
        {
            try
            {
                DoExecute(parameter);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected abstract void DoExecute(object? parameter);
    }
}
