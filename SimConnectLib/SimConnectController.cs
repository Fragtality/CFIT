using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Modules;
using CFIT.SimConnectLib.Modules.MobiFlight;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.SimConnectLib
{
    public class SimConnectController(ISimConnectConfig config, Type managerType, Type allocatorType, CancellationToken token, bool waitForSim = false)
    {
        public virtual ISimConnectConfig Config { get; } = config;
        public virtual SimConnectManager SimConnect { get; } = managerType.CreateInstance<SimConnectManager, ISimConnectConfig, Type, CancellationToken>(config, allocatorType, token);
        public virtual int CheckInterval { get; } = config.CheckInterval;
        public virtual CancellationToken Token { get; protected set; } = token;
        public virtual bool WaitForSim { get; } = waitForSim;
        public virtual bool IsSimRunning { get; protected set; } = false;
        public virtual bool IsMsfs2020Running => Sys.GetProcessRunning(Config.BinaryMsfs2020);
        public virtual bool IsMsfs2024Running => Sys.GetProcessRunning(Config.BinaryMsfs2024);
        public virtual bool IsSessionReady { get; protected set; } = false;
        public virtual bool IsCanceled { get; protected set; } = false;
        public virtual bool IsRunning { get; protected set; } = false;
        public virtual bool FirstRun { get; protected set; } = true;
        public virtual bool FirstConnect { get; protected set; } = true;
        protected virtual DateTime LastConnectionAttempt { get; set; } = DateTime.Now;
        protected virtual DateTime NextBinaryCheck { get; set; } = DateTime.MinValue;

        public event Action<SimConnectManager> OnSimStarted;
        public event Action<SimConnectManager> OnSimStopped;
        public event Action<SimConnectManager> OnSessionReady;
        public event Action<SimConnectManager> OnSessionEnded;

        public virtual SimConnectModule AddMobiModule(IMobiConfig config)
        {
            return SimConnect.AddModule(typeof(MobiModule), config);
        }

        public virtual bool CheckSimBinary()
        {
            if (NextBinaryCheck <= DateTime.Now)
            {
                IsSimRunning = IsMsfs2020Running || IsMsfs2024Running;
                NextBinaryCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.StaleTimeout);
            }

            return IsSimRunning;
        }

        public virtual async Task Run()
        {
            IsRunning = true;
            IsCanceled = false;

            try
            {
                if (WaitForSim)
                    await WaitSimLoop();

                if (CheckSimBinary())
                {
                    Logger.Debug($"Fire OnSimStarted");
                    _ = TaskTools.RunPool(() => OnSimStarted?.Invoke(SimConnect), Token);
                }

                LastConnectionAttempt = DateTime.Now;
                while (CheckSimBinary() && !Token.IsCancellationRequested && !SimConnect.QuitReceived && !IsCanceled)
                    await RunMainLoop();
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException && ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            await Reset();
            Logger.Information($"SimConnectController Task ended (simRunning: {IsSimRunning} | quitReceived: {SimConnect?.QuitReceived} | cancelled: {Token.IsCancellationRequested})");

            if (!IsCanceled)
            {
                Logger.Debug($"Fire OnSimStopped");
                _ = TaskTools.RunPool(() => OnSimStopped?.Invoke(SimConnect), Token);
            }
        }

        protected virtual async Task Reset()
        {
            if (!SimConnect.QuitReceived && SimConnect.IsSimConnectInitialized)
                await SimConnect.Disconnect();
            IsRunning = false;
            IsSessionReady = false;
            FirstRun = true;
            FirstConnect = true;
        }

        protected virtual async Task WaitSimLoop()
        {
            while (!CheckSimBinary() && !IsCanceled && !Token.IsCancellationRequested)
            {
                Logger.Information($"Sim not running - Retry in {Config.RetryDelay / 1000}s");
                await Task.Delay(Config.RetryDelay, Token);
            }
        }

        protected virtual async Task RunMainLoop()
        {
            if (!SimConnect.IsSimConnected)
            {
                var diff = DateTime.Now - LastConnectionAttempt;
                if (SimConnect.IsSimConnectInitialized && !SimConnect.IsReceiveRunning && diff >= TimeSpan.FromMilliseconds(Config.StaleTimeout))
                {
                    if (!FirstConnect)
                    {
                        LastConnectionAttempt = DateTime.Now;
                        Logger.Warning($"Stale Connection detected - force reconnect");
                        await SimConnect.Disconnect();
                        return;
                    }
                    else if (FirstConnect && diff >= TimeSpan.FromMilliseconds(Config.StaleTimeout * 6))
                    {
                        LastConnectionAttempt = DateTime.Now;
                        Logger.Warning($"Stale initial Connection detected - force reconnect");
                        await SimConnect.Disconnect();
                        return;
                    }
                }
                else if (!SimConnect.Connect())
                {
                    LastConnectionAttempt = DateTime.Now;
                    Logger.Information($"SimConnect not connected - Retry in {Config.RetryDelay / 1000}s");
                    if (!IsCanceled)
                        await Task.Delay(Config.RetryDelay, Token);
                    return;
                }
                else if (FirstRun)
                {
                    FirstRun = false;
                    await Task.Delay(500, Token);
                }
            }

            if (SimConnect.IsSimConnected && FirstConnect)
            {
                Logger.Debug($"First Connection established.");
                FirstConnect = false;
            }

            if (!SimConnect.IsReceiveRunning && SimConnect.IsSimConnected)
            {
                Logger.Warning($"Receive not running while Connection established! Reconnecting in {(Config.RetryDelay / 2) / 1000}s");
                await SimConnect.Disconnect();
                IsSessionReady = false;
                FirstRun = true;
                if (!IsCanceled)
                    await Task.Delay(Config.RetryDelay / 2, Token);
                return;
            }

            if (!IsSessionReady && SimConnect.IsSessionStarted)
            {
                Logger.Debug($"SESSION: ready (Camera {SimConnect.CameraState})");
                IsSessionReady = true;
                _ = TaskTools.RunPool(() => OnSessionReady?.Invoke(SimConnect), Token);
            }

            if (IsSessionReady && SimConnect.IsSessionStopped)
            {
                Logger.Debug($"SESSION: ended (Camera {SimConnect.CameraState})");
                IsSessionReady = false;
                _ = TaskTools.RunPool(() => OnSessionEnded?.Invoke(SimConnect), Token);
            }

            await SimConnect.CheckState();
            await SimConnect.CheckResources();

            if (!IsCanceled)
                await Task.Delay(CheckInterval, Token);
        }

        public virtual void Cancel()
        {
            IsCanceled = true;
        }
    }
}
