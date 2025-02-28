# CFIT - Installer

## 1 - Setup Project

1. New Project in VS22: WPF App (.NET Framework) => .NET Framework 4.8
1. Remove App.* and MainWindow.*
1. NuGet: CFIT.Installer, Costura.Fody | Update Packages | Restart VS
1. Project Properties - Build: 64bit Debug+Release, Change to Release
1. Make Payload Dir
1. Copy PackageApp and ExportInstaller Scripts to Project-Directory & Add
1. Add PackageApp as Pre-Script, Set Parameters
1. Add ExportInstaller as Post-Script, Set Parameters

<br/>

## 2 - Create Code Files

### 2.1 - Setup Configuration: Config.cs

```csharp
using CFIT.Installer.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Installer
{
    public class Config : ConfigBase
    {
        public override string ProductName { get { return "AppName"; } }
    }
}
```

- Override other Properties as needed (i.e. ExePath)
- Add Properties for certain Workers - i.e. Net or Mobi

<br/>

### 2.2 - Setup Tasks/Worker: WorkerManager.cs

```csharp
using CFIT.Installer.LibWorker;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Installer
{
    public class WorkerManager : WorkerManagerBase
    {
        public virtual Config Config { get { return BaseConfig as Config; } }

        public WorkerManager(ConfigBase config) : base(config)
        {
        }

        protected override void CreateInstallTasks()
        {
            // Tasks for Install Queue
            WorkerQueues[SetupMode.INSTALL].Enqueue(new WorkerInstallUpdate(Config));
        }

        protected override void CreateRemovalTasks()
        {
            // Tasks for Removal Queue
            WorkerQueues[SetupMode.REMOVE].Enqueue(new WorkerAppRemove<Config>(Config));
        }

        protected override void CreateUpdateTasks()
        {
            // Tasks for Update Queue
            WorkerQueues[SetupMode.INSTALL].Enqueue(new WorkerInstallUpdate(Config));
        }
    }
}
```

- Create Functions are called after CmdLine Parsing and Config Page - so ConfigOptions can be used to determine which Workers to queue
- Install and Update Queues need to be filled, even when there is no Difference (use common Function to setup both Queues)

<br/>

### 2.3 - Installer/Product Definition: Definition.cs

```csharp
using CFIT.Installer.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Installer
{
    public class Definition : ProductDefinition
    {
        public Config Config { get { return BaseConfig as Config; } }
        public WorkerManager WorkerManager { get { return BaseWorker as WorkerManager; } }

        public Definition(string[] args) : base(args)
        {

        }

        protected override void CreateConfig()
        {
            BaseConfig = new Config();
        }

        protected override void CreateWorker()
        {
            BaseWorker = new WorkerManager(Config);
        }
    }
}
```

- Override `CreateWindowBehavior` to modify WindowBehavior Instance (or to set with own derived Implementation)
- Override `CreatePageConfig` to add Config Page to Installer
- Override `ParseArguments` to hook into Command-Line Argument Parsing (e.g. for `WorkerPackagePaths`)

<br/>

### 2.4 - Entry-Point: AppMain.cs
```csharp
using CFIT.Installer;
using CFIT.Installer.UI.Behavior;
using System;

namespace Installer
{
    public class AppMain
    {
        public static InstallerApp<Definition, WindowBehavior, Config, WorkerManager> Instance { get; private set; }

        [STAThread]
        public static int Main(string[] args)
        {
            Instance = new InstallerApp<Definition, WindowBehavior, Config, WorkerManager>(new Definition(args));
            return Instance.Start();
        }
    }
}
```

<br/>

### 2.5 - Config Page: ConfigPage.cs (Optional)
```csharp
using CFIT.Installer.Product;
using CFIT.Installer.UI.Behavior;
using CFIT.Installer.UI.Config;

namespace Installer
{
    public class ConfigPage : PageConfig
    {
        public Config Config { get { return BaseConfig as Config; } }

        public override void CreateConfigItems()
        {
            ConfigItemHelper.CreateCheckboxDesktopLink(Config, ConfigBase.OptionDesktopLink, Items);
            ConfigItemHelper.CreateRadioAutoStart(Config, Items);

        }
    }
}
```

- Add ConfigItems to `Items` Property (i.e. Checkbox, Dropdown, Radio, ...)
- Set Default/Start-Value for used Options in Config

<br/>

## 3 - Complete Project Setup

1. Set App/Installer Icon in Project Properties
1. Do initial Build
1. Set `Installer.AppMain` as Entry-Point in Project Properties
1. Add AppPackage.zip, version.json and (optional) App Config to Project (Payload Subdirectory)
1. Set these Files as Embedded Resource
1. If Logo for Welcome Page: Add to Payload Directory as Resource (!). Set Path in `WelcomeLogoUri` (e.g. `"/Payload/icon.png"`)

<br/>