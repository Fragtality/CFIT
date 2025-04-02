using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.IO;

namespace CFIT.Installer.Product
{
    public enum SetupMode
    {
        INSTALL = 0,
        UPDATE,
        REMOVE
    }

    public abstract class ConfigBase
    {
        public static readonly string OptionSearchSimulators = "SearchSimulators";
        public static readonly string OptionPackagePaths = "MsfsPackagePaths";
        public static readonly string OptionDesktopLink = "DesktopLink";
        public static readonly string OptionAutoStartTargets = "AutoStartTargets";

        public abstract string ProductName { get; }
        public virtual string ProductBinary { get { return ProductName; } }
        public virtual string ProductExe { get { return $"{ProductBinary}.exe"; } }
        public virtual string ProductConfigFile { get { return $"AppConfig.json"; } }
        public virtual string ProductConfigPath { get { return Path.Combine(ProductPath, ProductConfigFile); } }
        public virtual string ProductExePath { get { return Path.Combine(ProductPath, ProductExe); } }
        public virtual string ProductPath { get { return $@"{Sys.FolderAppDataRoaming()}\{ProductName}"; } }
        public virtual string ProductVersionFile { get { return "version.json"; } }
        public virtual string ProductVersionPath { get { return Path.Combine(ProductPath, ProductVersionFile); } }
        public virtual bool HasVersionFile { get { return File.Exists(ProductVersionPath); } }
        public virtual string ProductVersionResource { get { return $"Installer.Payload.{ProductVersionFile}"; } }
        public virtual int ProductVersionFields { get; protected set; } = 3;
        public virtual ProductVersion ProductVersion { get; protected set; } = new ProductVersion();
        public virtual string Version { get { return $"{ProductVersion?.VersionParsed?.ToString(ProductVersionFields)}"; } }
        public virtual bool HasConfigFile { get { return CheckExistingConfigFile(); } }

        public virtual SetupMode Mode { get; set; }
#pragma warning disable
        public virtual Dictionary<string, object> OptionStore { get; } = new Dictionary<string, object>();
#pragma warning restore

        public ConfigBase()
        {
            GetAppVersion();
        }

        protected virtual void GetAppVersion()
        {
            ProductVersion = ProductVersion.GetProductVersionFromStream(ProductVersionResource) ?? new ProductVersion();
        }

        public virtual void CheckInstallerOptions()
        {

        }

        protected virtual bool CheckExistingConfigFile()
        {
            return File.Exists(ProductConfigPath);
        }

        public virtual bool HasProperty<T>(string key, out T value)
        {
            Logger.Debug($"Checking for Property '{key}'");
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
                Logger.Debug($"Trying to get Value for Property '{key}'");
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
                Logger.Debug($"Trying to set Value for Property '{key}'");
                GetType().GetProperty(key)?.SetValue(this, value);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual bool HasOption<T>(string key, out T value)
        {
            Logger.Debug($"Trying to get Value for Option '{key}'");
            if (OptionStore?.TryGetValue(key, out object outVal) == true)
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

        public virtual T GetOption<T>(string key)
        {
            Logger.Debug($"Trying to get Value for Option '{key}'");
            if (OptionStore?.TryGetValue(key, out object value) == true)
                return (T)value;
            else
                return default;
        }

        public virtual void SetOption<T>(string key, T value)
        {
            Logger.Debug($"Trying to set Value for Option '{key}' to '{value}'");
            if (OptionStore?.ContainsKey(key) == true)
                OptionStore[key] = (object)value;
            else
                OptionStore.Add(key, (object)value);
        }
    }
}
