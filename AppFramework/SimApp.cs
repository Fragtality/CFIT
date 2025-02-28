﻿using CFIT.AppFramework.AppConfig;
using CFIT.AppFramework.MessageService;
using CFIT.AppFramework.ResourceStores;
using CFIT.AppFramework.Services;
using CFIT.AppFramework.UI.NotifyIcon;
using CFIT.AppLogger;
using CFIT.AppTools;
using H.NotifyIcon;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CFIT.AppFramework
{
    public interface ISimApp
    {
        public ProductDefinitionBase DefinitionBase { get; }
        public CancellationTokenSource TokenSource { get; }
        public CancellationToken Token { get; }
        public int ExitCode { get; set; }
        public bool IsAppShutDown { get; }
        public Window AppWindow { get; }
        public AppMessageService MessageService { get; }
        public bool UpdateDetected { get; }

        public void RequestShutdown(int? exitCode = null);
    }

    public abstract class SimApp<TApp, TService, TConfig, TDefinition> : Application, ISimApp
        where TApp : SimApp<TApp, TService, TConfig, TDefinition>
        where TService : AppService<TApp, TService, TConfig, TDefinition>
        where TConfig : AppConfigBase<TDefinition>
        where TDefinition : ProductDefinitionBase
    {
        public static TApp Instance { get; private set; }
        public virtual TConfig Config { get; protected set; }
        public virtual TDefinition Definition { get { return AppConfigBase<TDefinition>.Definition; } }
        public virtual ProductDefinitionBase DefinitionBase { get { return Definition; } }
        public virtual TService AppService { get; protected set; }
        public virtual CancellationTokenSource TokenSource { get; } = new();
        public virtual CancellationToken Token { get { return TokenSource.Token; } }
        public virtual int ExitCode { get; set; } = 0;
        public virtual bool IsAppShutDown { get; protected set; } = false;
        public virtual Type AppWindowType { get; }
        public virtual Window AppWindow { get; protected set; }
        public virtual NotifyIcon NotifyIcon { get; }
        public virtual AppMessageService MessageService { get; protected set; }
        public virtual ReceiverStore ReceiverStore { get; protected set; }
        public virtual SimStore SimStore { get; protected set; }
        public virtual bool UpdateDetected { get; protected set; } = false;
        public virtual Version UpdateVersion { get; protected set; } = new();
        protected virtual bool IsDisposed { get; set; } = false;
        public const string defaultConfigArg = "--writeConfig";


        public SimApp(Type windowType, Type notifyModelType) : base()
        {
            AppWindowType = windowType;
            NotifyIcon = new NotifyIcon(this, notifyModelType);
            Instance = this as TApp;
        }

        protected virtual void InitApplication()
        {
            Directory.SetCurrentDirectory(Definition.ProductPath);

            InitStartupLog();
            Logger.Information($"Startup Log created!");
            Logger.Information($"CFIT.AppLogger Version: {AppLogger.LibVersion.Version}");
            Logger.Information($"CFIT.AppTools Version: {AppTools.LibVersion.Version}");
            Logger.Information($"CFIT.AppFramework Version: {LibVersion.Version}");
            Logger.Information($"CFIT.SimConnectManager Version: {SimConnectLib.LibVersion.Version}");
            Logger.Information($"Intializing '{Definition.ProductName}' App ({Definition.ProductVersionString}) ...");

            Logger.Information($"Loading AppConfig ({typeof(TConfig).Name}) ...");
            InitConfiguration();
        }

        protected virtual void InitStartupLog()
        {
            Logger.CreateAppLoggerSimple($"{Definition.ProductLogPath}/startup.log", LogLevel.Verbose);
        }

        protected virtual void InitConfiguration()
        {
            Config = AppConfigBase<TDefinition>.LoadConfiguration<TConfig>() ?? throw new NullReferenceException("AppConfig returned Null Reference!");
            Logger.Information($"Configuration loaded: v{Config.ConfigVersion} Definition: {typeof(TDefinition).Name}");
        }

        protected virtual bool CreateDefaultConfig(string[] args)
        {
            int idx = -1;
            if (args.Length == 2 && args[0] == defaultConfigArg)
                idx = 0;
            if (args.Length == 3 && args[1] == defaultConfigArg)
                idx = 1;
            if (idx == -1)
                return false;

            string path = args[idx+1];
            if (!Path.IsPathFullyQualified(path))
                return false;

            Console.WriteLine($"Writing Default Config to {path} ...");
            AppConfigBase<TDefinition>.SaveConfiguration(typeof(TConfig).CreateInstance<TConfig>(), path);
            return true;
        }

        public virtual int Start(string[] args)
        {
            if (CreateDefaultConfig(args))
                return 0;

            InitApplication();
            Logger.Information($"Running Application {Definition.ProductName} ({typeof(TApp).Name}) ...");
            int result = Run();

            return result;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            Logger.Information($"OnStartup received. Parsing Arguments (Count: {e?.Args?.Length}) ...");
            ParseArguments(e.Args);

            Logger.Information($"Checking Version ...");
            await CheckVersion();

            Logger.Information($"Intializing MessageService ...");
            InitMessageService();

            Logger.Information($"Intializing Tray Icon ...");
            InitSystray();

            Logger.Information($"Intializing App Service ({typeof(TService).Name}) ...");
            InitAppService();

            Logger.Information($"Creating SimConnect Hook ...");
            InitSimConnectHook();

            Logger.Information($"Creating Resource Stores ...");
            InitResourceStores();

            Logger.Information($"Starting App Log ({Config.LogFile}) ...");
            await StartAppLogger();

            Logger.Information($"Starting App Service ...");
            AppService.Start();

            Logger.Information($"Creating App Window ({AppWindowType.Name}) ...");
            InitAppWindow();
        }

        protected virtual void InitSystray()
        {
            NotifyIcon.Initialize();
            NotifyIcon.Show();
        }

        protected virtual void InitSimConnectHook()
        {
            AppService.SimConnect.CreateMessageHook();
        }

        protected virtual void InitMessageService()
        {
            MessageService = new(Token);
        }

        protected virtual void InitResourceStores()
        {
            SimStore = new(AppService.SimConnect);
            ReceiverStore = new(MessageService);
        }

        protected virtual void InitAppWindow()
        {
            AppWindow = AppWindowType.CreateInstance<Window>();

            if (Definition.MainWindowOverrideClose)
                AppWindow.Closing += (sender, e) => { e.Cancel = true; (sender as Window).Hide(); };

            if (Definition.MainWindowShowOnStartup)
                AppWindow.Show(disableEfficiencyMode: true);

            if (Definition.MainWindowSetTitle)
                AppWindow.Title = $"{AppWindow.Title} ({Definition.ProductVersionString})";
        }

        protected virtual void InitAppService()
        {
            AppService = typeof(TService).CreateInstance<TService, TConfig>(Config);
        }

        protected virtual void ParseArguments(string[] args)
        {

        }

        protected virtual async Task StartAppLogger()
        {
            Logger.CloseAndFlush();
            await Task.Delay(50);
            Logger.CreateAppLoggerRotated(Config);
            await Task.Delay(50);
            Logger.Information($"---------------------------------------------------------------------------");
            Logger.Information($"{Definition.ProductName} Startup completed: ({Definition.ProductVersionString})");
        }

        protected virtual async Task CheckVersion()
        {
            try
            {
                var appVersion = Definition.ProductVersion;

                HttpClient client = new()
                {
                    Timeout = TimeSpan.FromMilliseconds(1500)
                };
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

                string json = await client.GetStringAsync($"{Definition.ProductGitApi}/releases/latest");
                Logger.Debug($"json received: len {json?.Length}");
                JsonNode node = JsonSerializer.Deserialize<JsonNode>(json);
                string tag_name = node["tag_name"].ToString();
                if (tag_name.StartsWith('v'))
                    tag_name = tag_name[1..];

                if (Version.TryParse(tag_name, out Version repoVersion) && repoVersion > appVersion)
                {
                    UpdateDetected = true;
                    UpdateVersion = repoVersion;
                    Logger.Information($"New Version detected: {UpdateVersion.ToString(3)}");
                }
                else
                    Logger.Information($"Application up-to-date!");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logger.Error("---- APP CRASH ----");
            Logger.LogException(args.ExceptionObject as Exception);
            ExitCode = -1;
            ExecuteShutdown();
        }

        public virtual void RequestShutdown(int? exitCode = null)
        {
            if (IsAppShutDown)
                return;
            IsAppShutDown = true;

            if (exitCode != null)
                ExitCode = (int)exitCode;

            Logger.Information($"Shutdown Request received!");
            ExecuteShutdown();
        }

        protected virtual async void ExecuteShutdown()
        {
            Logger.Debug($"Cancel Operations.");
            CancelOperations();
            await Task.Delay(Definition.DelayShutdownCancel);

            Logger.Debug($"Free Resources.");
            FreeResources();
            await Task.Delay(Definition.DelayShutdownResources);

            Logger.Debug($"Signal Shutdown: {ExitCode}");
            try { Shutdown(ExitCode); }
            catch { Environment.Exit(ExitCode); }
        }

        protected virtual void CancelOperations()
        {
            if (TokenSource.IsCancellationRequested)
                return;

            TokenSource.Cancel();
        }

        protected virtual void FreeResources()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;

            SimStore.Clear();
            ReceiverStore.Clear();

            NotifyIcon.Dispose();
        }
    }
}
