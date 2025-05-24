using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using System;
using System.IO;
using static CFIT.Installer.LibFunc.FuncVersion;

namespace CFIT.Installer.LibFunc
{
    public class FuncStreamDeck
    {
        public virtual string Version { get; protected set; }
        public virtual string Path { get; protected set; }
        public virtual string BinaryPath { get; protected set; }
        public virtual bool IsValid { get { return !string.IsNullOrWhiteSpace(Version) && !string.IsNullOrWhiteSpace(Path); } }

        public static string PluginBinary { get; set; }
        public static string DeckPluginPath { get { return $@"{Sys.FolderAppDataRoaming()}\Elgato\StreamDeck\Plugins"; } }
        public static string DeckDefaultPath { get { return @"C:\Program Files\Elgato\"; } }
        public static string ProgDataPath { get; } = @"C:\ProgramData\Elgato\StreamDeck\STREAMDECKINTERNALSHM";
        public static string DeckBinaryName { get { return "StreamDeck"; } }
        public static string DeckBinaryExe { get { return $"{DeckBinaryName}.exe"; } }
        public static string DeckRegPathVersion { get { return @"HKEY_CURRENT_USER\SOFTWARE\Elgato Systems GmbH\StreamDeck"; } }
        public static string DeckRegValueVersion { get { return "last_started_streamdeck_version"; } }
        public static string DeckRegValueFolder { get { return "Folder"; } }
        public static string DeckRegPathInstall { get { return @"HKEY_CURRENT_USER\SOFTWARE\Elgato Systems GmbH\StreamDeck (Setup)"; } }
        public static string DeckRegValueInstall { get { return "installDir"; } }

        public FuncStreamDeck()
        {
            Version = Sys.GetRegistryValue<string>(DeckRegPathVersion, DeckRegValueVersion, null);
            Logger.Debug($"Returned Registry Value: {Version}");
            GetStreamDeckBinaryPath();
        }

        public static bool IsStreamDeckRunning(bool checkFile = true)
        {
            if (!checkFile)
                return Sys.GetProcessRunning(DeckBinaryName);
            else
                return Sys.GetProcessRunning(DeckBinaryName) || File.Exists(ProgDataPath);
        }

        public static bool IsDeckAndPluginRunning(bool checkFile = true)
        {
            if (!string.IsNullOrWhiteSpace(PluginBinary))
                return IsStreamDeckRunning(checkFile) && Sys.GetProcessRunning(PluginBinary);
            else
                return IsStreamDeckRunning(checkFile);
        }

        public static bool IsDeckOrPluginRunning(bool checkFile = true)
        {
            if (!string.IsNullOrWhiteSpace(PluginBinary))
                return IsStreamDeckRunning(checkFile) || Sys.GetProcessRunning(PluginBinary);
            else
                return IsStreamDeckRunning(checkFile);
        }

        protected virtual void GetStreamDeckBinaryPath()
        {
            Path = Sys.GetRegistryValue<string>(DeckRegPathVersion, DeckRegValueFolder, null);
            if (!string.IsNullOrWhiteSpace(Path))
            {
                BinaryPath = $@"{Path}{DeckBinaryName}\{DeckBinaryExe}";
                return;
            }

            Path = Sys.GetRegistryValue<string>(DeckRegPathInstall, DeckRegValueInstall, null);
            if (!string.IsNullOrWhiteSpace(Path))
            {
                BinaryPath = $@"{Path}{DeckBinaryExe}";
                return;
            }

            Logger.Warning("Could not get StreamDeck Folder from Registry! Assuming Default");
            Path = $@"{DeckDefaultPath}{DeckBinaryName}\";
            BinaryPath = $@"{Path}{DeckBinaryExe}";
        }

        public virtual bool CompareVersion(string version)
        {
            try
            {
                if (IsValid && CheckVersion(Version, VersionCompare.GREATER_EQUAL, version, out bool compareable) && compareable)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                TaskStore.CurrentTask.SetError(ex);
                return false;
            }
        }

        public virtual void StartSoftware()
        {
            Sys.StartProcess(BinaryPath, Path, null, true);
        }

        public virtual void StopSoftware()
        {
            Sys.RunCommand($"\"{BinaryPath}\" --quit", out _);
        }

        public virtual void KillSoftware()
        {
            Sys.KillProcess(DeckBinaryName);
        }
    }
}
