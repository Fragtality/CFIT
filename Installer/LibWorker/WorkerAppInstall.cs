﻿using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CFIT.Installer.LibWorker
{
    public abstract class WorkerAppInstall<C> : TaskWorker<C> where C : ConfigBase
    {
        public virtual string InstallerNamespace { get; set; } = "Installer";
        public virtual string InstallerAppPackage { get; set; } = "AppPackage.zip";
        public virtual string InstallerExtractDir { get; set; }
        public virtual bool InstallerDeleteOnUpdate { get; set; } = true;
        public virtual bool InstallerRunCreateConfig { get; set; } = true;
        public virtual bool InstallerRunFinalize { get; set; } = true;
        public virtual bool InstallerRunSaveVersion { get; set; } = true;
        public virtual bool InstallerUnblock { get; set; } = true;
        public virtual bool InstallerOverwrite { get; set; } = true;
        public virtual string InstallerSuccessMsg { get; set; }
        public virtual bool InstallerSummaryPath { get; set; } = true;

        public virtual List<string> FileExclusions { get; set; } = new List<string>();

        public WorkerAppInstall(C config) : base(config, $"Install {config?.ProductName}", "")
        {
            InstallerExtractDir = Config.ProductPath;
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = true;
            Logger.Debug("Setting Properties ...");
            SetProperties();
            Logger.Debug("Creating Exclusions ...");
            CreateFileExclusions();
        }

        protected virtual void SetProperties()
        {
            if (Config.Mode == SetupMode.INSTALL)
            {
                if (InstallerSummaryPath)
                    InstallerSuccessMsg = $"Successfully installed {Config.ProductName} to:";
                else
                    InstallerSuccessMsg = $"Successfully installed {Config.ProductName}!";
            }
            else
            {
                Model.Title = $"Update {Config.ProductName}";
                InstallerSuccessMsg = $"Successfully updated {Config.ProductName} to Version:\r\nv{Config.ProductVersion.VersionParsed.ToString(3)} ({Config.ProductVersion.Timestamp})";
            }

            SetPropertyFromConfig<string>("InstallerNamespace");
            SetPropertyFromConfig<string>("InstallerAppPackage");
            SetPropertyFromConfig<string>("InstallerExtractDir");
            SetPropertyFromConfig<string>("InstallerSuccessMsg");
            SetPropertyFromConfig<bool>("InstallerDeleteOnUpdate");
            SetPropertyFromConfig<bool>("InstallerRunCreateConfig");
            SetPropertyFromConfig<bool>("InstallerRunFinalize");
            SetPropertyFromConfig<bool>("InstallerRunSaveVersion");
            SetPropertyFromConfig<bool>("InstallerUnblock");
            SetPropertyFromConfig<bool>("InstallerOverwrite");
            SetPropertyFromConfig<bool>("InstallerSummaryPath");
        }

        protected virtual string GetAssemblyPath(string file)
        {
            return $"{InstallerNamespace}.Payload.{file}";
        }

        protected virtual Stream GetAppPackage()
        {
            return AssemblyTools.GetStreamFromAssembly(GetAssemblyPath(InstallerAppPackage));
        }

        protected virtual Stream GetAppConfig()
        {
            return AssemblyTools.GetStreamFromAssembly(GetAssemblyPath(Config.ProductConfigFile));
        }

        protected virtual Stream GetAppVersion()
        {
            return AssemblyTools.GetStreamFromAssembly(Config.ProductVersionResource);
        }

        protected abstract void CreateFileExclusions();

        protected abstract bool DeleteOldFiles();

        protected virtual bool ExtractAppPackage()
        {
            bool result = false;

            using (var stream = GetAppPackage())
            {
                if (stream == null)
                {
                    Model.SetError("Could not retrieve AppPackage Stream from Assembly!");
                    return result;
                }

                result = FuncZip.ExtractZipStream(InstallerExtractDir, stream, InstallerUnblock ? InstallerExtractDir : null, InstallerOverwrite, FileExclusions);
            }

            return result;
        }

        protected abstract bool CreateDefaultConfig();

        protected abstract bool FinalizeSetup();

        protected virtual bool SaveVersion()
        {
            bool result = false;

            using (var stream = GetAppVersion())
            {
                var file = File.Create(Config.ProductVersionPath);
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(file);
                file.Flush(true);
                file.Close();
                file.Dispose();
                stream.Dispose();
                result = File.Exists(Config.ProductVersionPath);
            }

            return result;
        }

        protected override async Task<bool> DoRun()
        {
            bool deleteSuccess;
            bool extractSuccess;
            bool configSuccess;
            bool finalizeSuccess;
            bool versionSuccess;
            await Task.Delay(0);

            if (Config.Mode == SetupMode.UPDATE && InstallerDeleteOnUpdate && !Token.IsCancellationRequested)
            {
                Model.Message = "Deleting old Binaries ...";
                deleteSuccess = DeleteOldFiles();
            }
            else
                deleteSuccess = !Token.IsCancellationRequested && (Config.Mode != SetupMode.UPDATE || !InstallerDeleteOnUpdate);

            if (deleteSuccess && !Token.IsCancellationRequested)
            {
                Model.Message = "Extracting App Binaries ...";
                extractSuccess = ExtractAppPackage();
            }
            else
                extractSuccess = false;

            if (extractSuccess && InstallerRunCreateConfig && !Config.HasConfigFile && !Token.IsCancellationRequested)
            {
                Model.Message = "Create Default Configuation ...";
                configSuccess = CreateDefaultConfig();
            }
            else
                configSuccess = !Token.IsCancellationRequested && (Config.HasConfigFile || !InstallerRunCreateConfig);

            if (configSuccess && InstallerRunFinalize && !Token.IsCancellationRequested)
            {
                Model.Message = "Finalizing Setup ...";
                finalizeSuccess = FinalizeSetup();
            }
            else
                finalizeSuccess = !Token.IsCancellationRequested && !InstallerRunFinalize;

            if (finalizeSuccess && InstallerRunSaveVersion && !Token.IsCancellationRequested)
            {
                Model.Message = "Saving App Version ...";
                versionSuccess = SaveVersion();
            }
            else
                versionSuccess = !Token.IsCancellationRequested && !InstallerRunSaveVersion;

            if (deleteSuccess && extractSuccess && configSuccess && finalizeSuccess && versionSuccess)
            {
                Model.SetSuccess(InstallerSuccessMsg, false, FontWeights.DemiBold);
                if (InstallerSummaryPath && Config.Mode == SetupMode.INSTALL)
                    Model.AddMessage(new TaskMessage(Config.ProductPath, true), false, false);
                return true;
            }
            else
            {
                Logger.Debug($"Failed! (delete {deleteSuccess} | extract {extractSuccess} | config {configSuccess} | finalize {finalizeSuccess} | version {versionSuccess})");
                return false;
            }
        }
    }
}
