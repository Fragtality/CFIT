using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.IO;

namespace CFIT.AppFramework.AppConfig
{
    public abstract class ProductDefinitionBase
    {
        public abstract int BuildConfigVersion { get; }
        public abstract string ProductName { get; }
        public virtual Version ProductVersion { get { return VersionTools.GetEntryAssemblyVersion(); } }
        public virtual string ProductTimestamp { get { return VersionTools.GetEntryAssemblyTimestamp(); } }
        public virtual string ProductVersionString { get { return $"{ProductVersion.ToString(3)}-{ProductTimestamp}"; } }
        public virtual string ProductAuthor { get { return "Fragtality"; } }
        public virtual string ProductBranch { get { return "master"; } }
        public virtual string ProductGitApi { get { return $"https://api.github.com/repos/{ProductAuthor}/{ProductName}"; } }
        public virtual string ProductVersionFileGit { get { return GetUrlGit("Installer/Payload/version.json"); } }
        public virtual string ProductVersionFileCdn { get { return GetUrlCdn("Installer/Payload/version.json"); } }
        public virtual string ProductBinary { get { return ProductName; } }
        public virtual string ProductExe { get { return $"{ProductBinary}.exe"; } }
        public virtual string ProductPath { get { return $@"{Sys.FolderAppDataRoaming()}\{ProductName}"; } }
        public virtual string ProductConfigFile { get { return $"AppConfig.json"; } }
        public virtual string ProductConfigPath { get { return Path.Join(ProductPath, ProductConfigFile); } }
        public virtual string ProductExePath { get { return Path.Join(ProductPath, ProductExe); } }
        public virtual string ProductLogPath { get { return "log"; } }
        public virtual string ProductInstallerLatest { get { return $"{ProductName}-Installer-latest.exe"; } }

        public virtual string GetUrlInstaller()
        {
            return GetUrlGit(ProductInstallerLatest);
        }

        public virtual string GetUrlGit(string path, string branch = "master")
        {
            return GetUrlGit(path, ProductAuthor, ProductName, branch);
        }

        public static string GetUrlGit(string path, string author, string product, string branch)
        {
            return $"https://raw.githubusercontent.com/{author}/{product}/refs/heads/{branch}/{path}";
        }

        public virtual string GetUrlCdn(string path, string branch = "master")
        {
            return GetUrlCdn(path, ProductAuthor, ProductName, branch);
        }

        public static string GetUrlCdn(string path, string author, string product, string branch)
        {
            return $"https://cdn.jsdelivr.net/gh/{author}/{product}@{branch}/{path}";
        }

        //Behaviors
        public virtual bool MainWindowShowOnStartup { get { return true; } }
        public virtual bool MainWindowSetTitle { get { return true; } }
        public virtual bool MainWindowOverrideClose { get { return true; } }
        public virtual bool SingleInstance { get { return false; } }
        public virtual bool WaitForSim { get { return true; } }
        public virtual bool RequireSimRunning { get { return true; } }
        public virtual int DelayShutdownCancel { get { return 1500; } }
        public virtual int DelayShutdownResources { get { return 750; } }
        public const int MessageCheckIntervalMs = 100;

        public virtual Dictionary<string, object> DefinitionStore { get; } = [];

        public ProductDefinitionBase()
        {

        }

        public virtual bool HasProperty<T>(string key, out T value)
        {
            Logger.Verbose($"Checking for Property '{key}'");
            if (this.HasProperty(key))
            {
                value = GetProperty<T>(key);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public virtual T GetProperty<T>(string key)
        {
            try
            {
                Logger.Verbose($"Trying to get Value for Property '{key}'");
                if (this.HasProperty(key))
                    return (T)GetType().GetProperty(key)?.GetValue(this, null);
                else
                    return default;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return default;
            }
        }

        public virtual void SetProperty<T>(string key, T value)
        {
            try
            {
                Logger.Verbose($"Trying to set Value for Property '{key}'");
                GetType().GetProperty(key)?.SetValue(this, value);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual bool HasDefinition<T>(string key, out T value)
        {
            Logger.Verbose($"Trying to get Value for Definition '{key}'");
            if (DefinitionStore?.TryGetValue(key, out object outVal) == true)
            {
                value = (T)outVal;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public virtual T GetDefinition<T>(string key)
        {
            Logger.Verbose($"Trying to get Value for Definition '{key}'");
            if (DefinitionStore?.TryGetValue(key, out object value) == true)
                return (T)value;
            else
                return default;
        }

        public virtual void SetDefinition<T>(string key, T value)
        {
            Logger.Verbose($"Trying to set Value for Definition '{key}' to '{value}'");
            if (DefinitionStore?.ContainsKey(key) == true)
                DefinitionStore[key] = (object)value;
            else
                DefinitionStore.Add(key, (object)value);
        }
    }
}
