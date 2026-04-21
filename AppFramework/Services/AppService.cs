using CFIT.AppFramework.AppConfig;
using CFIT.AppFramework.Messages;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows;

namespace CFIT.AppFramework.Services
{
    public abstract class AppService<TApp, TService, TConfig, TDefinition> : ServiceController<TApp, TService, TConfig, TDefinition>
        where TApp : SimApp<TApp, TService, TConfig, TDefinition>
        where TService : AppService<TApp, TService, TConfig, TDefinition>
        where TConfig : AppConfigBase<TDefinition>
        where TDefinition : ProductDefinitionBase
    {
        public static TService Instance { get; private set; }
        public virtual SimService<TApp, TService, TConfig, TDefinition> SimService { get; }
        public virtual SimConnectManager SimConnect { get { return SimService?.Manager; } }
        public virtual bool SimStoppedReceived { get; protected set; } = false;
        protected virtual ConcurrentDictionary<ServiceController<TApp, TService, TConfig, TDefinition>, bool> ServiceControllers { get; } = [];

        public AppService(TConfig config) : base(config)
        {
            Instance = this as TService;
            SimService = new(Config);
            ServiceControllers.Add(SimService);
            CreateServiceControllers();
        }

        /// <summary>
        /// Create <see cref="ServiceController{TApp, TService, TConfig, TDefinition}"/> that will automatically be started before the MainLoop (and stopped after)
        /// </summary>
        protected abstract void CreateServiceControllers();

        protected override Task DoInit()
        {
            MessageService.Subscribe<MsgSimStopped>(OnSimStopped);
            return Task.CompletedTask;
        }

        protected virtual Task OnSimStopped()
        {
            SimStoppedReceived = true;
            return Task.CompletedTask;
        }

        protected virtual bool RunCondition()
        {
            return !Definition.RequireSimRunning && !SimConnect.QuitReceived && !SimStoppedReceived || Definition.RequireSimRunning && SimService.Controller.IsSimRunning && !SimConnect.QuitReceived && !SimStoppedReceived;
        }

        protected override async Task DoRun()
        {
            if (!RunCondition())
            {
                MessageBox.Show($"No Simulator running - {Definition.ProductName} will exit now", "Sim not running", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Information($"Sim not running - cancel Execution");
                App.TokenSource.Cancel();
                App.RequestShutdown();
                return;
            }

            await StartServiceControllers();
            if (Definition.WaitForSim)
            {
                Logger.Information($"Waiting for Simulator ...");
                await MessageService.WaitReceived<MsgSimStarted>(Token, 1000);
            }

            if (IsExecutionAllowed)
                Logger.Debug($"Starting MainLoop ...");
            try
            {
                while (IsExecutionAllowed && RunCondition())
                    await MainLoop();
                Logger.Debug($"MainLoop ended. (ExecutionAllowed {IsExecutionAllowed} | RunCondition {RunCondition()})");
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                {
                    Logger.LogException(ex);
                    Logger.Error("MainLoop crashed!");
                    App.TokenSource.Cancel();
                }
            }

            if (!RunCondition() && !Token.IsCancellationRequested)
            {
                Logger.Debug($"RunCondition not met - request Shutdown");
                App.RequestShutdown();
            }

            await StopServiceControllers();
        }

        protected abstract Task MainLoop();

        protected virtual Task StartServiceControllers()
        {
            return CallOnControllers((controller) => controller.Start());
        }

        protected virtual Task StopServiceControllers()
        {
            return CallOnControllers((controller) => controller.Stop());
        }

        protected virtual void CallOnControllers(Action<ServiceController<TApp, TService, TConfig, TDefinition>> action)
        {
            foreach (var controller in ServiceControllers.Keys)
                action.Invoke(controller);
        }

        protected virtual async Task CallOnControllers(Func<ServiceController<TApp, TService, TConfig, TDefinition>, Task> action)
        {
            foreach (var controller in ServiceControllers.Keys)
                await action.Invoke(controller);
        }

        protected override Task DoCleanup()
        {
            MessageService.Unsubscribe<MsgSimStopped>(OnSimStopped);
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                    CallOnControllers((controller) => controller.Dispose());
            }

            base.Dispose(disposing);
        }
    }
}
