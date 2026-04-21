
using CFIT.AppTools;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public class WorkerDesktopLinkCreate<C> : TaskWorker<C> where C : ConfigBase
    {
        public WorkerDesktopLinkCreate(C config, string title = "Desktop Link", string message = "Creating Link ...") : base(config, title, message)
        {
            Model.DisplayInSummary = true;
            Model.DisplayCompleted = true;
        }

        protected virtual bool CreateLink()
        {
            return Sys.CreateLink(Config.ProductName, Config.ProductExePath, $"Start {Config.ProductName}");
        }

        protected override Task<bool> DoRun()
        {
            bool result = CreateLink();
            if (result)
                Model.SetSuccess("Link placed on Desktop!");

            return Task.FromResult(result);
        }
    }
}
