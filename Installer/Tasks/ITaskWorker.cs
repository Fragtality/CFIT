using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.Installer.Tasks
{
    public interface ITaskWorker
    {
        CancellationToken Token { get; }

        bool IsRunning { get; }
        bool HasFinished { get; }
        bool IsSuccess { get;  }
        bool IsFailed { get; }

        bool IgnoreFailed { get; }
        TaskModel Model { get; }
        Queue<ITaskWorker> LinkedTasks { get; }

        Task<bool> Run(CancellationToken token);
    }
}
