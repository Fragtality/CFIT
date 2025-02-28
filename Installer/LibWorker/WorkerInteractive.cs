using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public class WorkerInteractive<C> : TaskWorker<C> where C : ConfigBase
    {
        public virtual List<TaskLink> Links { get { return Model?.Links; } }
        public virtual TaskInteraction Interaction { get; protected set; }
        public virtual Enum ResponseSuccess { get; set; }
        public virtual string MessageSuccess { get; set; }
        public virtual string MessageFailed { get; set; }

        public WorkerInteractive(C config, string title, string message = "") : base(config, title, message)
        {
            Interaction = new TaskInteraction(Model);
        }

        public virtual void AddInteraction(string title, Enum response, Action callback = null)
        {
            Interaction.AddInteraction(title, response, callback);
        }

        public virtual void AddInteraction(string title, Enum response, string url, string args = "")
        {
            Interaction.AddInteraction(title, response, url, args);
        }

        protected override async Task<bool> DoRun()
        {
            bool isSuccess = (await Interaction.WaitOnResponse(Token)).CompareTo(ResponseSuccess) == 0;
            if (isSuccess)
                Model.SetSuccess(MessageSuccess);
            else
                Model.SetError(MessageFailed);

            return isSuccess;
        }
    }
}
