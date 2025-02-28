using CFIT.AppLogger;
using CFIT.Installer.Product;
using CFIT.Installer.UI;
using CFIT.Installer.UI.Behavior;
using System;
using System.Windows;

namespace CFIT.Installer
{
    public class InstallerApp<D, B, C, W> : Application where D : ProductDefinition where B : WindowBehavior where C : ConfigBase where W : WorkerManagerBase
    {
        public static D BaseDefinition { get; protected set; }
        public static C Config { get { return BaseDefinition?.BaseConfig as C; } }
        public static W Worker { get { return BaseDefinition?.BaseWorker as W; } }
        public static B Behavior { get { return BaseDefinition?.BaseBehavior as B; } }
        public static InstallerWindow InstallerWindow { get; protected set; }

        public InstallerApp(D definition) : base()
        {
            BaseDefinition = definition;
            Logger.Information("Creating Main Window ...");
            InstallerWindow = new InstallerWindow(definition);
            MainWindow = InstallerWindow;
        }

        public int Start()
        {
            Logger.Information($"Running Installer App ...");
            int result = Run();

            Logger.DestroyLoggerSession();
            return result;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            Logger.Information($"Startup received. Showing Main Window ...");
            MainWindow.Show();
        }

        public void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logger.LogException(args.ExceptionObject as Exception);
        }
    }
}
