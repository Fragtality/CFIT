﻿using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CFIT.Installer.LibWorker
{
    public class WorkerFsuipc6<C> : TaskWorker<C>, IWorkerUpdates where C : ConfigBase
    {
        public virtual bool Fsuipc6Required { get; set; } = true;
        public virtual string Fsuipc6Version { get; set; }
        public virtual bool Fsuipc6AllowBeta { get; set; } = false;
        public virtual Simulator Fsuipc6Simulator { get; set; } = Simulator.P3DV4;
        public virtual string Fsuipc6Url { get; set; }
        public virtual string Fsuipc6Installer { get; set; } = "FSUIPC6";
        public virtual string Fsuipc6InstallerArchive { get { return $"{Fsuipc6Installer}.zip"; } }
        public virtual string Fsuipc6InstallerBinary { get { return $"Install_{Fsuipc6Installer}.exe"; ; } }

        public virtual bool ShowUpdateInSummary { get; set; } = true;
        public virtual bool ShowUpdateCompleted { get; set; } = true;
        public virtual bool DisplayInSummary { get { return Model.DisplayInSummary; } set { Model.DisplayInSummary = value; } }
        public virtual bool DisplayPinned { get { return Model.DisplayPinned; } set { Model.DisplayPinned = value; } }
        public virtual bool DisplayCompleted { get { return Model.DisplayCompleted; } set { Model.DisplayCompleted = value; } }

        public WorkerFsuipc6(C config, Simulator sim) : base(config, $"FSUIPC6 [{sim}]", "Check State and Version of FSUIPC6 ...")
        {
            Model.DisplayInSummary = false;
            Model.DisplayCompleted = true;

            Fsuipc6Simulator = sim;
            SetPropertyFromConfig<bool>("Fsuipc6Required");
            SetPropertyFromConfig<string>("Fsuipc6Version");
            SetPropertyFromConfig<bool>("Fsuipc6AllowBeta");
            SetPropertyFromConfig<string>("Fsuipc6Url");
            SetPropertyFromConfig<string>("Fsuipc6Installer");
        }

        protected override async Task<bool> DoRun()
        {
            bool result = false;

            if (string.IsNullOrWhiteSpace(Fsuipc6Url) || string.IsNullOrWhiteSpace(Fsuipc6Version) || string.IsNullOrWhiteSpace(Fsuipc6Installer))
            {
                Model.SetError($"Url, Installer or Version not set - abort!");
                return result;
            }

            if (FuncFsuipc6.CheckVersion(Fsuipc6Simulator, Fsuipc6Version, Fsuipc6AllowBeta))
            {
                Model.SetSuccess($"FSUIPC6 at or above minimum Version {Fsuipc6Version}!");
                result = true;
            }
            else
            {
                if (ShowUpdateInSummary)
                    Model.DisplayInSummary = true;
                if (ShowUpdateCompleted)
                    Model.DisplayCompleted = true;

                Model.AddMessage(new TaskMessage($"FSUIPC6 below minimum Version {Fsuipc6Version}!", false, FontWeights.DemiBold), true, false);
                Model.State = TaskState.WAITING;

                if (!SetupPossible())
                {
                    Model.AddMessage("Installation not possible while Prepar3D is running!", false, false, false, FontWeights.DemiBold);
                    Model.AddMessage("Click Retry when Prepar3D is closed (or cancel the Installation).");
                    var interaction = new TaskInteraction(Model);
                    interaction.AddInteraction("Retry", InteractionResponse.RETRY);

                    if (await interaction.WaitOnResponse(Token, InteractionResponse.RETRY) && SetupPossible())
                    {
                        Model.Links.Clear();
                        result = await InstallFsuipc6();
                    }
                    else
                    {
                        Model.Links.Clear();
                        Model.SetError("Prepar3D still running!");
                    }
                }
                else
                    result = await InstallFsuipc6();
            }

            return result || !result && !Fsuipc6Required;
        }

        public static bool SetupPossible()
        {
            return !FuncFsuipc6.IsRunning();
        }

        protected virtual async Task<bool> InstallFsuipc6()
        {
            Model.SetState($"Downloading FSUIPC Installer ...", TaskState.WAITING);
            string archivePath = await FuncIO.DownloadFile(Token, Fsuipc6Url, Fsuipc6InstallerArchive);
            if (string.IsNullOrWhiteSpace(archivePath))
            {
                Model.SetError("Could not download FSUIPC Installer!");
                return false;
            }
            string workDir = Path.GetDirectoryName(archivePath);

            Model.Message = "Extracting Installer Archive ...";
            string installerDir = $@"{workDir}\{Fsuipc6Installer}";
            FuncIO.DeleteDirectory(installerDir, true);
            if (!FuncZip.ExtractZipFile(workDir, archivePath))
            {
                Model.SetError("Error while extracting FSUIPC Installer!");
                return false;
            }

            Model.Message = $"Running FSUIPC Installer - manual Interaction required ...";
            string binPath = $@"{installerDir}\{Fsuipc6InstallerBinary}";
            if (!File.Exists(binPath))
            {
                Model.SetError("Could not locate the Installer Binary!");
                return false;
            }

            Sys.RunCommand(binPath, out _);
            await Task.Delay(1000, Token);

            try
            {
                FuncIO.DeleteFile(archivePath);
                FuncIO.DeleteDirectory(installerDir, true);
            }
            catch { }

            if (FuncFsuipc6.CheckVersion(Fsuipc6Simulator, Fsuipc6Version, Fsuipc6AllowBeta))
            {
                Model.SetSuccess($"FSUIPC Version {Fsuipc6Version} was installed/updated successfully!");
                return true;
            }
            else
            {
                Model.SetError($"FSUIPC not at target Version after Setup!");
                return false;
            }

        }
    }
}
