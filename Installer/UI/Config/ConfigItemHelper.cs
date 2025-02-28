using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using CFIT.Installer.Product;
using System.Collections.Generic;

namespace CFIT.Installer.UI.Config
{
    public static class ConfigItemHelper
    {
        public static void CreateCheckboxDesktopLink(ConfigBase config, string optionKey, List<ConfigItem> items, string text = null, string name = "Desktop Link")
        {
            if (string.IsNullOrEmpty(text))
                text = $"Create Link for {config.ProductName} on Desktop";

            items.Add(new ConfigItemCheckbox(name, text, optionKey, config));
            if (config.Mode == SetupMode.INSTALL)
                config.SetOption(optionKey, true);
            else
                config.SetOption(optionKey, false);
        }

        public static void CreateRadioAutoStart(ConfigBase config, List<ConfigItem> items, string name = "Setup Auto-Start", string optionKey = null)
        {
            if (string.IsNullOrEmpty(optionKey))
                optionKey = ConfigBase.OptionAutoStartTargets;

            var options = new Dictionary<int, string>()
            {
                { (int)SimAutoStart.NOCHANGE, "Do not change Configuration" },
                { (int)SimAutoStart.NOAUTO, "Disable Auto-Start" },
                { (int)SimAutoStart.FSUIPC, "Auto-Start with FSUIPC7" },
                { (int)(SimAutoStart.MSFS2020 | SimAutoStart.MSFS2024), "Auto-Start with MSFS 2020/2024" },
            };
            if (FuncMsfs.CheckInstalledMsfs(Simulator.MSFS2020))
                options.Add((int)SimAutoStart.MSFS2020, "Auto-Start with MSFS 2020 only");
            if (FuncMsfs.CheckInstalledMsfs(Simulator.MSFS2024))
                options.Add((int)SimAutoStart.MSFS2024, "Auto-Start with MSFS 2024 only");
            config.SetOption(optionKey, SimAutoStart.NOCHANGE);

            items.Add(new ConfigItemRadio(name, options, optionKey, config));
        }
    }
}
