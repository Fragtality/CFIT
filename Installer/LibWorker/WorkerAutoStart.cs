using CFIT.AppLogger;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using System;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public enum SimAutoStart
    {
        NOCHANGE = 1,
        NOAUTO = 2,
        FSUIPC = 4,
        MSFS2020 = 8,
        MSFS2024 = 16,
    }

    public class WorkerAutoStart<C> : TaskWorker<C> where C : ConfigBase
    {
        public virtual SimAutoStart AutoStartTargets { get; set; } = SimAutoStart.NOCHANGE;
        public virtual string AutoStartSuccessMsg { get; set; }

        protected virtual int Fails { get; set; } = 0;

        public WorkerAutoStart(C config, string title = "Setup Auto-Start", string message = "") : base(config, title, message)
        {
            Model.DisplayInSummary = true;
            Model.DisplayCompleted = true;
            SetPropertyFromOption<SimAutoStart>(ConfigBase.OptionAutoStartTargets);
            AutoStartSuccessMsg = $"Auto-Start configured for {Config.ProductName}!";
        }

        protected virtual void RemoveAutoStart(SimAutoStart flag, Func<bool> func)
        {
            Logger.Debug($"Check Removal for '{flag}'");
            if (!AutoStartTargets.HasFlag(flag))
                if (func?.Invoke() == false)
                    Fails++;
        }

        protected virtual void AddUpdateAutoStart(SimAutoStart flag, Func<bool> func, string message = null)
        {
            if (!AutoStartTargets.HasFlag(flag))
                return;

            if (string.IsNullOrEmpty(message))
                message = $"Add/Update {flag} Auto-Start Entry ...";
            Model.Message = message;

            if (func?.Invoke() == false)
                Fails++;
        }

        protected override async Task<bool> DoRun()
        {
            bool result = false;
            await Task.Delay(0);

            if (AutoStartTargets.HasFlag(SimAutoStart.NOCHANGE))
            {
                Model.SetSuccess($"No Changes to Auto-Start!");
                Model.DisplayInSummary = false;
                return true;
            }
            else if (AutoStartTargets.HasFlag(SimAutoStart.NOAUTO))
            {
                Model.Message = "Remove Auto-Start Entries ...";
                RemoveAutoStart(SimAutoStart.FSUIPC, () => { return FuncFsuipc7.AutoStartRemove(Config.ProductExe); });
                RemoveAutoStart(SimAutoStart.MSFS2020, () => { return FuncMsfs.AutoStartRemove(Simulator.MSFS2020, Config.ProductExe); });
                RemoveAutoStart(SimAutoStart.MSFS2024, () => { return FuncMsfs.AutoStartRemove(Simulator.MSFS2024, Config.ProductExe); });
                result = Fails == 0;
                if (!result)
                    Model.SetError("Auto-Start Removal failed!");
                else
                    Model.SetSuccess($"Auto-Start removed for {Config.ProductName}!");
                return result;
            }
            else
            {
                Model.Message = "Remove unused Auto-Start Entries ...";
                RemoveAutoStart(SimAutoStart.FSUIPC, () => { return FuncFsuipc7.AutoStartRemove(Config.ProductExe); });
                RemoveAutoStart(SimAutoStart.MSFS2020, () => { return FuncMsfs.AutoStartRemove(Simulator.MSFS2020, Config.ProductExe); });
                RemoveAutoStart(SimAutoStart.MSFS2024, () => { return FuncMsfs.AutoStartRemove(Simulator.MSFS2024, Config.ProductExe); });

                if (Fails == 0)
                {
                    AddUpdateAutoStart(SimAutoStart.FSUIPC, () => { return FuncFsuipc7.AutoStartAddUpdate(Config.ProductExePath, Config.ProductExe); });
                    if (FuncMsfs.CheckInstalledMsfs(Simulator.MSFS2020))
                        AddUpdateAutoStart(SimAutoStart.MSFS2020, () => { return FuncMsfs.AutoStartAddUpdate(Simulator.MSFS2020, Config.ProductExePath, Config.ProductExe, Config.ProductName); });
                    if (FuncMsfs.CheckInstalledMsfs(Simulator.MSFS2024))
                        AddUpdateAutoStart(SimAutoStart.MSFS2024, () => { return FuncMsfs.AutoStartAddUpdate(Simulator.MSFS2024, Config.ProductExePath, Config.ProductExe, Config.ProductName); });

                    result = Fails == 0;
                    if (!result)
                        Model.SetError("Auto-Start Configuration failed!");
                }
                else
                    Model.SetError("Auto-Start Removal failed!");

                if (result)
                {
                    Model.SetSuccess(AutoStartSuccessMsg);
                    return true;
                }
                else
                    return false;
            }

        }
    }
}
