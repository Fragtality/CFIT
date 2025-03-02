using CFIT.AppLogger;
using CFIT.Installer.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.Installer.Tasks
{
    public abstract class TaskWorker<C> : ITaskWorker where C : ConfigBase
    {
        public virtual CancellationToken Token { get; protected set; }

        public virtual bool IsRunning { get; protected set; } = false;
        public virtual bool HasFinished { get; protected set; } = false;
        public virtual bool IsSuccess { get; protected set; } = false;
        public virtual bool IsFailed { get; protected set; } = false;

        public virtual bool IgnoreFailed { get; set; } = false;

        public virtual C Config { get; protected set; }
        public virtual TaskModel Model { get; protected set; }
        public Queue<ITaskWorker> LinkedTasks { get; } = new Queue<ITaskWorker>();

#pragma warning disable
        public TaskWorker(C config, string title, string message = "")
#pragma warning restore
        {
            Config = config;
            Model = new TaskModel(title, message);
        }

        protected virtual void SetPropertyFromConfig<T>(string key)
        {
            if (Config?.HasProperty(key, out T confValue) == true && GetType()?.GetProperties()?.Any(p => p.Name == key) == true)
            {
                GetType()?.GetProperty(key)?.SetValue(this, confValue);
            }
        }

        protected virtual void SetPropertyFromOption<T>(string key)
        {
            if (Config?.HasOption(key, out T confValue) == true && GetType()?.GetProperties()?.Any(p => p.Name == key) == true)
            {
                GetType()?.GetProperty(key)?.SetValue(this, confValue);
            }
        }

        protected virtual bool RunCondition()
        {
            return true;
        }

        protected abstract Task<bool> DoRun();

        public virtual async Task<bool> Run(CancellationToken token)
        {
            IsRunning = true;
            bool doRun = RunCondition();
            Token = token;
            if (doRun)
                TaskStore.Add(Model);
            else
            {
                Model.DisplayInSummary = false;
                Logger.Debug($"RunCondition() returned false - skip Worker");
            }

            try
            {
                if (doRun)
                {
                    Logger.Debug($"Running Task '{Model.Title}' ...");
                    IsSuccess = await DoRun();
                }
                else
                    IsSuccess = true;
            }
            catch (Exception ex)
            {
                Model.SetError(ex);
                IsFailed = true;
            }

            IsRunning = false;
            HasFinished = true;
            Model.IsCompleted = true;
            Model.IsSuccess = IsSuccess || IgnoreFailed;
            if (doRun)
                Logger.Debug($"Task '{Model.Title}' finished!");
            else
                Logger.Debug($"Task '{Model.Title}' was skipped!");
            return Model.IsSuccess;
        }
    }
}
