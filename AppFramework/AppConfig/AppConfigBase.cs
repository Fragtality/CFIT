using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using Serilog;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFIT.AppFramework.AppConfig
{
    public abstract class AppConfigBase<TDefinition>() : ILoggerConfig, ISimConnectConfig
        where TDefinition : ProductDefinitionBase
    {
        [JsonIgnore]
        public static TDefinition Definition { get; } = typeof(TDefinition).CreateInstance<TDefinition>();
        [JsonIgnore]
        public static string ConfigFile { get { return Definition.ProductConfigPath; } }

        //Config Versioning
        [JsonIgnore]
        public static int BuildConfigVersion { get; set; } = 1;
        public int ConfigVersion { get; set; } = BuildConfigVersion;

        //ILoggerConfig
        [JsonIgnore]
        public virtual string LogDirectory { get { return Definition.ProductLogPath; } }
        [JsonIgnore]
        public virtual string LogFile { get { return $"{Definition.ProductName}.log"; } }
        public virtual RollingInterval LogInterval { get; set; } = RollingInterval.Day;
        public virtual int SizeLimit { get; set; } = 1024 * 1024;
        public virtual int LogCount { get; set; } = 3;
        public virtual string LogTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message} {NewLine}";
        public virtual LogLevel LogLevel { get; set; } = LogLevel.Debug;

        //ISimConnectConfig
        [JsonIgnore]
        public virtual string ClientName { get { return Definition.ProductName; } }
        public virtual uint IdBase { get; set; } = 500;
        public virtual int RetryDelay { get; set; } = 30 * 1000;
        public virtual int StaleTimeout { get; set; } = 3 * 1000;
        public virtual int CheckInterval { get; set; } = 500;
        [JsonIgnore]
        public virtual bool CreateWindow { get { return true; } }
        [JsonIgnore]
        public virtual int MsgSimConnect { get { return 0x2004; } }
        [JsonIgnore]
        public virtual int MsgConnectRequest { get { return 0x2005; } }
        public virtual bool VerboseLogging { get; set; } = false;
        public virtual uint SizeVariables { get; set; } = 5000;
        public virtual uint SizeEvents { get; set; } = 5000;
        public virtual uint SizeInputEvents { get; set; } = 5000;
        public virtual string BinaryMsfs2020 { get; set; } = "FlightSimulator";
        public virtual string BinaryMsfs2024 { get; set; } = "FlightSimulator2024";

        //App
        //...


        public static TConfig LoadConfiguration<TConfig>() where TConfig : AppConfigBase<TDefinition>
        {
            bool emptyConfig = false;
            if (!File.Exists(ConfigFile))
            {
                Logger.Information($"Creating empty Configuration File ...");
                File.WriteAllText(ConfigFile, "{}", Encoding.UTF8);
                emptyConfig = true;
            }

            TConfig config = JsonSerializer.Deserialize<TConfig>(File.ReadAllText(ConfigFile), JsonOptions.JsonSerializerOptions) ?? throw new NullReferenceException("The App Configuration is null!");

            if (config.ConfigVersion < BuildConfigVersion)
            {
                Logger.Information($"Migrating Configuration from Version '{config.ConfigVersion}' to '{BuildConfigVersion}'");

                config.UpdateConfiguration(BuildConfigVersion);

                config.ConfigVersion = BuildConfigVersion;
                config.SaveConfiguration();
            }
            else if (config.ConfigVersion > BuildConfigVersion)
            {
                Logger.Warning($"Existing Configuration Version '{config.ConfigVersion}' is higher than Build Version '{BuildConfigVersion}'!");
            }

            if (emptyConfig)
            {
                config.SaveConfiguration();
                Logger.Information($"Saved new default Configuration.");
            }

            Logger.Information($"Initializing Configuration ...");
            config.InitConfiguration();

            return config;
        }

        public abstract void SaveConfiguration(); //SaveConfiguration<TYPE>(this, ConfigFile);

        public static void SaveConfiguration<TConfig>(TConfig config = null, string configFile = null) where TConfig : AppConfigBase<TDefinition>
        {
            config ??= default;
            configFile ??= ConfigFile;
            File.WriteAllText(configFile, JsonSerializer.Serialize(config, JsonOptions.JsonWriteOptions));
        }

        protected abstract void UpdateConfiguration(int buildConfigVersion);

        protected abstract void InitConfiguration();
    }
}
