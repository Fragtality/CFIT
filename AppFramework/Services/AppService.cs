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
        protected virtual ConcurrentDictionary<ServiceController<TApp, TService, TConfig, TDefinition>, bool> ServiceControllers { get; } = [];

        public AppService(TConfig config) : base(config)
        {
            Instance = this as TService;
            SimService = new(Config);
            ServiceControllers.Add(SimService);
            CreateServiceControllers();
        }

        protected abstract void CreateServiceControllers();

        protected override void InitReceivers()
        {
            base.InitReceivers();
            ReceiverStore.Add<MsgSimStarted>();
        }

        protected virtual bool RunCondition()
        {
            return !Definition.RequireSimRunning && !SimConnect.QuitReceived || Definition.RequireSimRunning && SimService.Controller.IsSimRunning && !SimConnect.QuitReceived;
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

            StartServiceControllers();
            if (Definition.WaitForSim)
            {
                Logger.Information($"Waiting for Simulator ...");
                await ReceiverStore.Get<MsgSimStarted>().ReceiveAsync();
            }

            Logger.Debug($"Starting MainLoop ...");
            while (IsExecutionAllowed && RunCondition())
                await MainLoop();
            Logger.Debug($"MainLoop ended. (ExecutionAllowed {IsExecutionAllowed} RunCondition {RunCondition()})");

            StopServiceControllers();

            if (!RunCondition())
            {
                IsResettable = false;
                if (!Token.IsCancellationRequested)
                {
                    Logger.Debug($"RunCondition not met - request Shutdown");
                    App.RequestShutdown();
                }
            }
        }

        protected abstract Task MainLoop();

        protected virtual void StartServiceControllers()
        {
            CallOnControllers((controller) => controller.Start());
        }

        protected virtual void StopServiceControllers()
        {
            CallOnControllers((controller) => controller.Stop());
        }

        protected virtual void CallOnControllers(Action<ServiceController<TApp, TService, TConfig, TDefinition>> action)
        {
            foreach (var controller in ServiceControllers.Keys)
                action.Invoke(controller);
        }

        protected override void FreeResources()
        {
            base.FreeResources();
            CallOnControllers((controller) => controller.Dispose());
            ReceiverStore.Remove<MsgSimStarted>();
        }
    }
}
