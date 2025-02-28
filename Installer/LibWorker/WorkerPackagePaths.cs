using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public class WorkerPackagePaths<C> : TaskWorker<C> where C : ConfigBase
    {
#pragma warning disable
        public List<Simulator> SearchSimulators { get; set; } = new List<Simulator>();
#pragma warning restore

        public WorkerPackagePaths(C config, string title = "Package Paths", string message = "") : base(config, title, message)
        {
            Model.DisplayInSummary = false;
            SetPropertyFromOption<List<Simulator>>(ConfigBase.OptionSearchSimulators);
        }

        public static void ParseSimArguments(string[] args, ConfigBase config)
        {
            var list = new List<Simulator>();

            if (Sys.HasArgument(args, "--2020"))
            {
                list.Add(Simulator.MSFS2020);
                Logger.Information("Argument '--2020' passed!");
            }
            if (Sys.HasArgument(args, "--2024"))
            {
                list.Add(Simulator.MSFS2024);
                Logger.Information("Argument '--2024' passed!");
            }

            if (list.Count == 0)
            {
                list.Add(Simulator.MSFS2020);
                list.Add(Simulator.MSFS2024);
            }
            config.SetOption(ConfigBase.OptionSearchSimulators, list);
        }

        protected void CheckSim(Simulator sim, Dictionary<Simulator, string[]> dict)
        {
            if (SearchSimulators?.Contains(sim) == true)
            {
                Model.Message = $"Searching Package Path for {sim} ...";
                if (FuncMsfs.CheckInstalledMsfs(sim, out string[] paths))
                {
                    dict.Add(sim, paths);
                    Logger.Debug($"Added {paths?.Length} Paths for Simulator {sim}");
                }
            }
        }

        protected override async Task<bool> DoRun()
        {
            await Task.Delay(0);
            var packagePaths = new Dictionary<Simulator, string[]>();
            foreach (var sim in SearchSimulators)
                CheckSim(sim, packagePaths);

            if (packagePaths.Any(kv => kv.Value.Length > 0))
            {
                Config.SetOption(ConfigBase.OptionPackagePaths, packagePaths);
                Model.SetSuccess($"Found {packagePaths.Sum(kv => kv.Value.Length)} Package Paths!");
                return true;
            }
            else
            {
                Model.SetError("No Package Paths found!");
                return false;
            }
        }
    }
}
