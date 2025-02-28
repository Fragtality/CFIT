using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public class WorkerCountdown<C> : TaskWorker<C> where C : ConfigBase
    {
        public virtual int Countdown { get; protected set; }
        public virtual string TextWaiting { get; protected set; }
        public virtual string TextEnded { get; protected set; }

        public WorkerCountdown(C config, string title, string waiting, int seconds, string ended = "") : base(config, title, "")
        {
            TextWaiting = waiting;
            Countdown = seconds;
            TextEnded = ended;
        }

        protected override async Task<bool> DoRun()
        {
            await TaskWaiter.CountdownWaiter(Model, TextWaiting, Countdown, Token, TaskState.COMPLETED);
            if (!Token.IsCancellationRequested && !string.IsNullOrWhiteSpace(TextEnded))
                Model.AddMessage(TextEnded, true);
            return true;
        }
    }
}
