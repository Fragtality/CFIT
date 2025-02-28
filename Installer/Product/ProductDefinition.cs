using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.UI.Behavior;
using System;
using System.Collections.Generic;
using System.IO;

namespace CFIT.Installer.Product
{
    public enum InstallerPages
    {
        WELCOME = 0,
        CONFIG,
        SETUP,
        SUMMARY
    }

    public abstract class ProductDefinition
    {
        public virtual ConfigBase BaseConfig { get; protected set; }
        public virtual WorkerManagerBase BaseWorker { get; protected set; }
        public virtual WindowBehavior BaseBehavior { get; protected set; }
#pragma warning disable
        public virtual Dictionary<InstallerPages, IPageBehavior> PageBehaviors { get; protected set; } = new Dictionary<InstallerPages, IPageBehavior>();
#pragma warning restore
        public virtual bool IsProductInstalled { get { return CheckExistingInstallation(); } }
        public virtual bool IsRunning { get { return CheckIsRunning(); } }
        public virtual ProductVersion AppVersion { get; protected set; } = null;

        public ProductDefinition(string[] args)
        {
            Console.WriteLine("Creating Config ...");
            CreateConfig();
            Logger.CreateAppLoggerSession($"{BaseConfig?.ProductName}-Installer.log", LogLevel.Debug);
            Logger.Information($"Config created. Log created.");
            Logger.Information($"CFIT.AppLogger Version: {AppLogger.LibVersion.Version}");
            Logger.Information($"CFIT.AppTools Version: {AppTools.LibVersion.Version}");
            Logger.Information($"CFIT.Installer Version: {LibVersion.Version}");
            Logger.Information($"Packaged App Version: {BaseConfig?.ProductVersion?.VersionParsed?.ToString(BaseConfig?.ProductVersionFields ?? 3)} {BaseConfig?.ProductVersion?.Timestamp}");
            Logger.Debug($"Checking SetupMode ...");
            SetSetupMode();
            Logger.Debug($"Parsing Arguments ...");
            ParseArguments(args);
            Logger.Debug($"Creating Worker ...");
            CreateWorker();
            Logger.Debug($"Creating Behavior ...");
            CreateWindowBehavior();
            Logger.Debug($"Creating Page Welcome ...");
            CreatePageWelcome();
            Logger.Debug($"Creating Page Config ...");
            CreatePageConfig();
            Logger.Debug($"Creating Page Setup ...");
            CreatePageSetup();
            Logger.Debug($"Creating Page Summary ...");
            CreatePageSummary();
        }



        protected abstract void CreateConfig();

        protected abstract void CreateWorker();

        protected virtual bool CheckExistingInstallation()
        {
            return Directory.Exists(BaseConfig.ProductPath);
        }

        protected virtual bool CheckIsRunning()
        {
            return Sys.GetProcessRunning(BaseConfig.ProductBinary);
        }

        protected virtual void SetSetupMode()
        {
            if (IsProductInstalled)
                BaseConfig.Mode = SetupMode.UPDATE;
            else
                BaseConfig.Mode = SetupMode.INSTALL;
        }

        protected virtual void ParseArguments(string[] args)
        {
            if (Sys.HasArgument(args, "--debug"))
            {
                Logger.SessionKeepFile = true;
                Logger.Information("Installer running in Debug Mode!");
            }
        }

        protected virtual void CreateWindowBehavior()
        {
            BaseBehavior = new WindowBehavior();
        }

        protected virtual void CreatePageWelcome()
        {
            PageBehaviors.Add(InstallerPages.WELCOME, new PageWelcome());
        }

        protected virtual void CreatePageConfig()
        {
            
        }

        protected virtual void CreatePageSetup()
        {
            PageBehaviors.Add(InstallerPages.SETUP, new PageSetup());
        }

        protected virtual void CreatePageSummary()
        {
            PageBehaviors.Add(InstallerPages.SUMMARY, new PageSummary());
        }
    }
}
