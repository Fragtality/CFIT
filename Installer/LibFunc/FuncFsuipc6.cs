using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using System;
using System.Collections.Generic;

namespace CFIT.Installer.LibFunc
{
    public static class FuncFsuipc6
    {
        public static Dictionary<Simulator, string> Fsuipc6RegPaths { get; } = new Dictionary<Simulator, string>()
        {
            {
                Simulator.P3DV4,
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC6v4"
            },
            {
                Simulator.P3DV5,
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC6v5"
            },
            {
                Simulator.P3DV6,
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC6v6"
            },
        };

        public static string Fsuipc6RegValueVersion { get; } = "DisplayVersion";
        public static string Fsuipc6RegValueDirectory { get; } = "InstallDir";
        public static string Fsuipc6Binary { get; } = "Prepar3D";

        public static bool IsRunning()
        {
            return Sys.GetProcessRunning(Fsuipc6Binary);
        }

        public static bool CheckVersion(Simulator simulator, string targetVersion, bool allowBeta = false, FuncVersion.VersionCompare compare = FuncVersion.VersionCompare.GREATER_EQUAL)
        {
            bool result = false;

            if (string.IsNullOrWhiteSpace(targetVersion))
                return result;

            try
            {
                string regVersion = GetRegValueFsuipc6(simulator, Fsuipc6RegValueVersion);

                if (!string.IsNullOrWhiteSpace(regVersion))
                {
                    Logger.Debug($"Comparing installed '{regVersion}' vs target '{targetVersion}'");
                    result = FuncVersion.CheckVersion(regVersion, compare, targetVersion, out bool compareable) && compareable;
                    if (result && !allowBeta && regVersion?.Contains("beta") == true)
                        result = false;
                }
                else
                    Logger.Information("FSUIPC6 not installed - could not get Version!");
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return result;
        }

        public static string GetRegValueFsuipc6(Simulator simulator, string key, object defaultValue = null)
        {
            string result = Sys.GetRegistryValue<string>(Fsuipc6RegPaths[simulator], key, defaultValue);
            Logger.Debug($"Result for Key '{key}' on Path '{Fsuipc6RegPaths[simulator]}' - '{result}'");

            return result;
        }
    }
}
