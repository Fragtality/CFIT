using CFIT.AppLogger;
using CFIT.Installer.Product;
using CFIT.Installer.UI.Behavior;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CFIT.Installer.UI
{
    public partial class InstallerWindow : Window
    {
        public ProductDefinition BaseDefinition { get; private set; }
        public ConfigBase BaseConfig { get { return BaseDefinition?.BaseConfig; } }
        public IPageBehavior CurrentPage { get; private set; } = null;
        public bool IsActivated { get; private set; } = false;
        public WindowBehavior BaseBehavior { get { return BaseDefinition?.BaseBehavior; } }
        public Dictionary<InstallerPages, IPageBehavior> PageBehaviors { get { return BaseDefinition?.PageBehaviors; } }
        public Action<InstallerWindow> ActionLeft { get; set; } = null;
        public Action<InstallerWindow> ActionRight { get; set; } = null;
        public static string WindowTitle { get; set; } = "";

        public InstallerWindow(ProductDefinition definition) : base()
        {
            BaseDefinition = definition;
            MinHeight = BaseBehavior?.TargetHeight ?? 608;
            InitializeComponent();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Logger.Debug("Received Window activated");
            if (!IsActivated)
            {
                LoadWelcome();
                Title = BaseBehavior?.FuncGetTitle?.Invoke(this);
                WindowTitle = Title;
                BaseBehavior?.CallbackOnActivated?.Invoke(this);
                IsActivated = true;
                Logger.Debug("Window set to activated");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Debug("Received Window closing");
            if (BaseDefinition?.BaseWorker?.IsRunning == true)
                BaseDefinition.BaseWorker.TokenSource.Cancel();
            SetPage();
        }

        public void LoadWelcome()
        {
            SetPage(InstallerPages.WELCOME);
        }

        public bool HasPage(InstallerPages page)
        {
            return PageBehaviors?.ContainsKey(page) == true;
        }

        public void SetPage(InstallerPages page = (InstallerPages)(-1))
        {
            if (CurrentPage != null)
            {
                Logger.Debug($"Deactivating current Page ({CurrentPage.GetType().Name}) ...");
                CurrentPage.Deactivate();
                GridMain.Children.Remove(CurrentPage.ContentRef);
                PanelHeader.Children.Clear();
                PanelFooter.Children.Clear();
                ActionLeft = null;
                ActionRight = null;
                ButtonLeft.IsEnabled = true;
                ButtonRight.IsEnabled = true;
                CurrentPage = null;
            }

            Logger.Debug($"Setting Page for '{page}' ...");
            if (PageBehaviors?.TryGetValue(page, out IPageBehavior behavior) == true && behavior != null)
            {
                behavior.Activate(this);
                if (behavior.ContentRef != null)
                {
                    Grid.SetColumn(behavior.ContentRef, 0);
                    Grid.SetRow(behavior.ContentRef, 1);
                    GridMain.Children.Add(behavior.ContentRef);
                }
                CurrentPage = behavior;
            }            
        }

        private void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            ActionLeft?.Invoke(this);
        }

        private void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            ActionRight?.Invoke(this);
        }
    }
}
