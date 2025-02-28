using CFIT.AppFramework.AppConfig;
using CFIT.AppFramework.MessageService;
using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CFIT.AppFramework.Services
{
    public abstract class ServiceController<TApp, TService, TConfig, TDefinition> : IDisposable
        where TApp : SimApp<TApp, TService, TConfig, TDefinition>
        where TService : AppService<TApp, TService, TConfig, TDefinition>
        where TConfig : AppConfigBase<TDefinition>
        where TDefinition : ProductDefinitionBase
    {
        public virtual TApp App { get { return SimApp<TApp, TService, TConfig, TDefinition>.Instance; } }
        public virtual string Name { get { return GetType().Name; } }
        public virtual TConfig Config { get; }
        public virtual TDefinition Definition { get { return AppConfigBase<TDefinition>.Definition; } }
        public virtual AppMessageService MessageService { get { return App.MessageService; } }
        public virtual ReceiverStore ReceiverStore { get { return App.ReceiverStore; } }
        public virtual SimStore SimStore { get { return App.SimStore; } }
        public virtual CancellationToken Token { get { return App.Token; } }
        public virtual DispatcherTimer UpdateTimer { get; }
        public virtual int UpdateIntervalMs { get; } = -1;
        public virtual Task ServiceTask { get; protected set; }
        public virtual bool AwaitCompletion { get; set; } = false;
        public virtual bool IsResettable { get; set; } = false;
        public virtual uint ResetLimit { get; set; } = uint.MaxValue;
        public virtual uint ResetCounter { get; set; } = 0;
        public virtual bool IsInitialized { get; protected set; } = false;
        public virtual bool IsTaskRunning { get { return ServiceTask?.Status <= TaskStatus.RanToCompletion; } }
        protected virtual bool ExecutionFlag { get; set; } = true;
        public virtual bool IsExecutionAllowed { get { return IsTaskRunning && !Token.IsCancellationRequested && ExecutionFlag && !isDisposed; } }
        protected bool isDisposed = false;

        public ServiceController(TConfig config)
        {
            Config = config;

            if (UpdateIntervalMs > 0)
            {
                UpdateTimer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(UpdateIntervalMs)
                };
                UpdateTimer.Tick += OnTick;
            }
            else
                UpdateTimer = null;
        }

        protected virtual void OnTick(object? sender, EventArgs e)
        {
            if (!IsExecutionAllowed)
                UpdateTimer?.Stop();
        }

        protected virtual void Init()
        {
            if (!IsInitialized)
                InitReceivers();

            IsInitialized = true;
        }

        protected virtual void InitReceivers()
        {

        }

        protected abstract Task DoRun();

        protected virtual async void Run()
        {
            try
            {
                Logger.Debug($"Service Task '{Name}' started.");
                do
                {
                    await DoRun();
                }
                while (Reset());

                if (Token.IsCancellationRequested)
                {
                    FreeResources();
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            Logger.Debug($"Service Task '{Name}' ended.");
        }

        protected virtual void ResetState()
        {

        }

        protected virtual bool Reset()
        {
            ResetState();
            ExecutionFlag = IsResettable;
            ResetCounter++;
            bool result = IsResettable && ResetCounter < ResetLimit && !Token.IsCancellationRequested;
            if (result)
                Logger.Debug($"Service Task '{Name}' resetted.");
            return result;
        }

        protected virtual void FreeResources()
        {
            
        }

        public virtual void Start()
        {
            try
            {
                ExecutionFlag = true;
                ServiceTask = new(Run, Token, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning);
                Logger.Debug($"Initializing Service '{Name}' ...");
                Init();
                Logger.Debug($"Starting Service '{Name}' ...");
                ServiceTask.Start();
                UpdateTimer?.Start();

                if (AwaitCompletion)
                    ServiceTask.Wait(Token);
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        public virtual void Stop()
        {
            Logger.Debug($"Stop requested for '{Name}'");
            ExecutionFlag = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    if (ExecutionFlag)
                        Stop();
                    FreeResources();
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
