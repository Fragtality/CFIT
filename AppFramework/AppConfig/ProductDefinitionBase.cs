using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.IO;

namespace CFIT.AppFramework.AppConfig
{
    public abstract class ProductDefinitionBase
    {
        public abstract string ProductName { get; }
        public virtual Version ProductVersion { get { return VersionTools.GetEntryAssemblyVersion(); } }
        public virtual string ProductTimestamp { get { return VersionTools.GetEntryAssemblyTimestamp(); } }
        public virtual string ProductVersionString { get { return $"{ProductVersion.ToString(3)}-{ProductTimestamp}"; } }
        public virtual string ProductAuthor { get { return "Fragtality"; } }
        public virtual string ProductGitApi { get { return $"https://api.github.com/repos/{ProductAuthor}/{ProductName}"; } }
        public virtual string ProductBinary { get { return ProductName; } }
        public virtual string ProductExe { get { return $"{ProductBinary}.exe"; } }
        public virtual string ProductPath { get { return $@"{Sys.FolderAppDataRoaming()}\{ProductName}"; } }
        public virtual string ProductConfigFile { get { return $"AppConfig.json"; } }
        public virtual string ProductConfigPath { get { return Path.Join(ProductPath, ProductConfigFile); } }
        public virtual string ProductExePath { get { return Path.Join(ProductPath, ProductExe); } }
        public virtual string ProductLogPath { get { return "log"; } }

        //Behaviors
        public virtual bool MainWindowShowOnStartup { get { return true; } }
        public virtual bool MainWindowSetTitle { get { return true; } }
        public virtual bool MainWindowOverrideClose { get { return true; } }
        public virtual bool WaitForSim { get { return true; } }
        public virtual bool RequireSimRunning { get { return true; } }
        public virtual int DelayShutdownCancel { get { return 500; } }
        public virtual int DelayShutdownResources { get { return 250; } }
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
