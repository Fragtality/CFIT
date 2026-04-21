using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace CFIT.AppFramework.UI.NotifyIcon
{
    public partial class NotifyIconViewModel : ObservableObject
    {
        public virtual string AssemblyName { get { return Assembly.GetEntryAssembly().GetName().Name; } }
        public virtual ISimApp SimApp { get; }
        public virtual string IconNormal { get { return "UI.Icons.AppIcon.ico"; } }
        public virtual string IconUpdate { get { return "UI.Icons.AppIconUpdate.ico"; } }
        public virtual string TextToolTip { get { return "Left-click to toggle Window, Right-click for Menu"; } }
        public virtual string TextToggleWindow { get { return "Toggle Window"; } }
        public virtual string TextLogDir { get { return "Log Directory"; } }
        public virtual string TextExitApp { get { return "Exit"; } }
        public virtual bool NoLeftClickDelay { get { return true; } }
        public virtual IRelayCommand CommandLeftClick { get { return ToggleWindowCommand; } }
        public virtual IRelayCommand CommandToggleWindow { get { return ToggleWindowCommand; } }
        public virtual IRelayCommand CommandLogDir { get { return LogDirCommand; } }
        public virtual IRelayCommand CommandExit { get { return ExitAppCommand; } }
        public virtual List<Tuple<string, IRelayCommand>> Items { get; } = [];
        public virtual Icon AppIcon { get; protected set; }

        public NotifyIconViewModel(ISimApp simApp) : base()
        {
            SimApp = simApp;
            CreateItems();
        }

        protected virtual void CreateItems()
        {

        }

        public virtual void Initialize()
        {
            AppIcon = GetIconNormal();
            SimApp.NewBuild += (_, _) => SetUpdateIcon();
            SimApp.NewVersion += (_, _) => SetUpdateIcon();
        }

        protected virtual void SetUpdateIcon()
        {
            AppIcon = GetIconUpdate();
        }

        public virtual Icon GetIconNormal()
        {
            return GetIcon($"{AssemblyName}.{IconNormal}");
        }

        public virtual Icon GetIconUpdate()
        {
            return GetIcon($"{AssemblyName}.{IconUpdate}");
        }

        protected virtual Icon GetIcon(string assemblyPath)
        {
            using var stream = AssemblyTools.GetStreamFromAssembly(assemblyPath);
            return new Icon(stream);
        }

        [RelayCommand]
        public virtual void ToggleWindow()
        {
            try
            {
                Logger.Debug($"Toggle Window via SysTray - Visible: {SimApp.AppWindow.IsVisible}");
                if (SimApp.AppWindow.IsVisible)
                    SimApp.AppWindow.Hide(enableEfficiencyMode: false);
                else if (!SimApp.IsAppShutDown)
                {
                    SimApp.AppWindow.Show(disableEfficiencyMode: true);
                    SimApp.AppWindow.Activate();
                }
            }
            catch { }
        }

        [RelayCommand]
        public virtual void LogDir()
        {
            try { Process.Start(new ProcessStartInfo(Path.Join(SimApp.DefinitionBase.ProductPath, SimApp.DefinitionBase.ProductLogPath)) { UseShellExecute = true }); } catch { }
        }

        [RelayCommand]
        public virtual void ExitApp()
        {
            Logger.Debug("Exit Request via SysTray");
            try { SimApp.RequestShutdown(); } catch { }
        }
    }
}
