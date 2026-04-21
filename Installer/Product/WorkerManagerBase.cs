using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.Installer.Product
{
    public abstract class WorkerManagerBase
    {
        public virtual ConfigBase BaseConfig { get; protected set; }
        public virtual Dictionary<SetupMode, Queue<ITaskWorker>> WorkerQueues { get; } = new Dictionary<SetupMode, Queue<ITaskWorker>>();
        public virtual Queue<ITaskWorker> TaskWorkers { get; protected set; } = new Queue<ITaskWorker>();
        public virtual CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();
        public virtual CancellationToken Token { get { return TokenSource.Token; } }

        public virtual bool IsRunning { get; set; } = false;
        public virtual bool IsCompleted { get; set; } = false;
        public virtual bool IsSuccess { get; set; } = false;
        public virtual bool LastResult { get; protected set; } = true;
        public WorkerManagerBase(ConfigBase config)
        {
            BaseConfig = config;
        }

        protected virtual void CreateTasks()
        {
            Logger.Debug($"Creating Queues for Workers ...");
            WorkerQueues.Add(SetupMode.INSTALL, new Queue<ITaskWorker>());
            WorkerQueues.Add(SetupMode.UPDATE, new Queue<ITaskWorker>());
            WorkerQueues.Add(SetupMode.REMOVE, new Queue<ITaskWorker>());

            Logger.Debug($"Creating Install Workers ...");
            CreateInstallTasks();
            Logger.Debug($"Creating Update Workers ...");
            CreateUpdateTasks();
            Logger.Debug($"Creating Removal Workers ...");
            CreateRemovalTasks();
            Logger.Debug($"Tasks created!");
        }

        protected abstract void CreateInstallTasks();

        protected abstract void CreateUpdateTasks();

        protected abstract void CreateRemovalTasks();

        protected virtual bool CheckSuccess()
        {
            return LastResult;
        }

        public virtual void Run()
        {
            _ = TaskTools.RunPool(DoRun, Token);
        }

        protected virtual async Task DoRun()
        {
            IsRunning = true;
            try
            {
                Logger.Information($"Worker Manager has started! Creating Tasks ...");
                CreateTasks();
                switch (BaseConfig?.Mode)
                {
                    case SetupMode.UPDATE:
                        TaskWorkers = WorkerQueues[SetupMode.UPDATE];
                        break;
                    case SetupMode.REMOVE:
                        TaskWorkers = WorkerQueues[SetupMode.REMOVE];
                        break;
                    default:
                        TaskWorkers = WorkerQueues[SetupMode.INSTALL];
                        break;
                }

                Logger.Information($"Running {TaskWorkers?.Count} Workers from Queue {BaseConfig?.Mode} ...");
                LastResult = true;
                await RunQueue(TaskWorkers);
                IsSuccess = CheckSuccess();
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
                IsSuccess = false;
            }

            IsRunning = false;
            IsCompleted = true;
            Logger.Information($"Worker is completed (Success: {IsSuccess})");
        }

        protected virtual async Task<bool> RunQueue(Queue<ITaskWorker> workerQueue)
        {
            while (LastResult && workerQueue.Count > 0 && Token.IsCancellationRequested == false)
            {
                var runner = workerQueue.Dequeue();
                if (runner != null)
                {
                    LastResult = await runner.Run(Token);
                    if (LastResult && runner.LinkedTasks?.Count > 0)
                    {
                        Logger.Information($"Running {runner.LinkedTasks?.Count} linked Workers from previous Worker ...");
                        LastResult = await RunQueue(runner.LinkedTasks);
                    }
                }
                else
                    LastResult = false;
            }

            return LastResult;
        }
    }
}
