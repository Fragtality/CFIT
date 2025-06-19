using CFIT.AppFramework.AppConfig;
using CFIT.AppFramework.Messages;
using CFIT.AppLogger;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Definitions;
using H.NotifyIcon;
using System.Threading.Tasks;

namespace CFIT.AppFramework.Services
{
    public class SimService<TApp, TService, TConfig, TDefinition> : ServiceController<TApp, TService, TConfig, TDefinition>
        where TApp : SimApp<TApp, TService, TConfig, TDefinition>
        where TService : AppService<TApp, TService, TConfig, TDefinition>
        where TConfig : AppConfigBase<TDefinition>
        where TDefinition : ProductDefinitionBase
    {
        public virtual SimConnectController Controller { get; }
        public virtual SimConnectManager Manager { get { return Controller.SimConnect; } }

        public SimService(TConfig config) : base(config)
        {
            Controller = new SimConnectController(Config, typeof(SimConnectManager), typeof(IdAllocator), Token, Definition.WaitForSim);
            InitSimConnect();
        }

        protected virtual void InitSimConnect()
        {
            Manager.WindowHook.WindowShow = (hook) => { hook.HelperWindow.Show(disableEfficiencyMode: true); };
            Manager.WindowHook.WindowHide = (hook) => { hook.HelperWindow.Hide(enableEfficiencyMode: false); };
        }

        protected override async Task DoRun()
        {
            Controller.Run();
            while (Controller.IsRunning && !Token.IsCancellationRequested)
                await Task.Delay(Config.CheckInterval, Token);
        }

        protected override Task FreeResources()
        {
            base.FreeResources();
            Manager.Dispose();
            return Task.CompletedTask;
        }

        protected override Task InitReceivers()
        {
            Controller.OnSimStarted += SendMessage<MsgSimStarted>;
            Controller.OnSimStopped += SendMessage<MsgSimStopped>;
            Controller.OnSessionReady += SendMessage<MsgSessionReady>;
            Controller.OnSessionEnded += SendMessage<MsgSessionEnded>;
            return Task.CompletedTask;
        }

        protected virtual void SendMessage<TMessage>(SimConnectManager sender) where TMessage : MessageSimulator
        {
            Logger.Debug($"Controller Event => Send '{typeof(TMessage).Name}'");
            MessageService.Send(MessageSimulator.Create<TMessage>(sender));
        }

        public override Task Stop()
        {
            base.Stop();
            Controller.Cancel();
            return Task.CompletedTask;
        }
    }
}
