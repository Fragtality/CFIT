using CFIT.AppFramework.AppConfig;
using CFIT.AppFramework.MessageService;
using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.AppFramework.Services
{
    public abstract class ServiceController<TApp, TService, TConfig, TDefinition>(TConfig config) : IDisposable
        where TApp : SimApp<TApp, TService, TConfig, TDefinition>
        where TService : AppService<TApp, TService, TConfig, TDefinition>
        where TConfig : AppConfigBase<TDefinition>
        where TDefinition : ProductDefinitionBase
    {
        public virtual TApp App => SimApp<TApp, TService, TConfig, TDefinition>.Instance;
        public virtual string Name => GetType().Name;
        public virtual TConfig Config { get; } = config;
        public virtual TDefinition Definition => AppConfigBase<TDefinition>.Definition;
        public virtual AppMessageService MessageService => App.MessageService;
        public virtual SimStore SimStore => App.SimStore;
        public virtual CancellationToken Token => App.Token;
        public virtual Task ServiceTask { get; protected set; }
        public virtual bool AwaitCompletion { get; set; } = false;
        public virtual uint ResetLimit { get; set; } = uint.MaxValue;
        public virtual uint ResetCounter { get; set; } = 0;
        public virtual bool IsInitialized { get; protected set; } = false;
        public virtual bool IsRunning { get; protected set; } = false;
        public virtual bool IsTaskRunning => ServiceTask?.Status <= TaskStatus.RanToCompletion;
        protected virtual bool ExecutionFlag { get; set; } = false;
        protected virtual bool IsInitializing { get; set; } = false;
        protected virtual bool IsCleaning { get; set; } = false;
        public virtual bool IsExecutionAllowed => IsTaskRunning && !Token.IsCancellationRequested && ExecutionFlag && !isDisposed && ResetCounter < ResetLimit;
        protected bool isDisposed = false;


        /// <summary>
        /// Initialize/Create Resources before Start/Run
        /// IsInitialized will set by the Base Class
        /// </summary>
        /// <returns></returns>
        protected abstract Task DoInit();

        protected virtual async Task Init()
        {
            if (!IsInitialized && !IsInitializing)
            {
                IsInitializing = true;
                await DoInit();
                IsInitializing = false;
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Two Options to implement:
        /// 1 - DoRun() has its own dedicated Loop - but then MUST monitor IsExecutionAllowed
        /// 2 - Let Run() loop and signal via DoReset() for continuation
        /// Uncaught Exceptions will end Run()
        /// </summary>
        /// <returns></returns>
        protected abstract Task DoRun();

        protected virtual async Task Run()
        {
            IsRunning = true;
            try
            {
                Logger.Debug($"Service Task '{Name}' active.");
                do
                {
                    await DoRun();
                }
                while (await Reset());
                Logger.Debug($"Service Task '{Name}' ended.");
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                {
                    Logger.LogException(ex);
                    Logger.Error($"Service Task '{Name}' crashed!");
                }
            }

            try
            {
                if (!IsExecutionAllowed)
                {
                    await Cleanup();
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            IsRunning = false;
        }

        /// <summary>
        /// Reset the State after DoRun has ended (normally/controlled) before DoRun can run again
        /// </summary>
        /// <returns>true => Call DoRun again</returns>
        protected virtual Task<bool> DoReset()
        {
            return Task.FromResult(false);
        }

        protected virtual async Task<bool> Reset()
        {
            ResetCounter++;
            bool result = await DoReset() && IsExecutionAllowed;
            if (result)
                Logger.Debug($"Service Task '{Name}' resetted.");
            else
                ExecutionFlag = false;
            return result;
        }

        /// <summary>
        /// Clear/Free used Resources, called when:
        /// - Stop(true) is requested
        /// - IsExecutionAllowed not met (i.e. Token is canceled)
        /// - Instance is disposed (and IsInitialized)
        /// IsInitialized will be reset by the Base Class
        /// </summary>
        /// <returns></returns>
        protected abstract Task DoCleanup();

        protected virtual async Task Cleanup()
        {
            if (IsInitialized && !IsCleaning)
            {
                IsCleaning = true;
                await DoCleanup();
                Logger.Debug($"Resources for '{Name}' released.");
                IsCleaning = false;
            }
            IsInitialized = false;
        }

        public virtual async Task Start()
        {
            try
            {
                if (ExecutionFlag)
                    throw new InvalidOperationException($"Service '{Name}' is already running!");
                ExecutionFlag = true;
                Logger.Debug($"Initializing Service '{Name}' ...");
                await Init();
                Logger.Debug($"Starting Service '{Name}' ...");
                ServiceTask = Task.Factory.StartNew(async () => await Run(), Token, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

                if (AwaitCompletion)
                    await ServiceTask;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        public virtual async Task Stop()
        {
            if (ServiceTask == null || ExecutionFlag == false)
                return;

            Logger.Debug($"Stopping Service '{Name}' ...");
            ExecutionFlag = false;
            try
            {
                await ServiceTask;
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    Logger.LogException(ex);
            }
            Logger.Debug($"Service '{Name}' stopped.");

            try
            {
                await Cleanup();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            ServiceTask = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing && ExecutionFlag)
                {
                    Stop().GetAwaiter().GetResult();
                }
                isDisposed = true;
            }
        }

        public virtual void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
