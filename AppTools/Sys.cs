using CFIT.AppLogger;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Input;


namespace CFIT.AppTools
{
    public static class Sys
    {
        public static string FolderAppDataRoaming()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        public static string FolderAppDataLocal()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        public static bool HasArgument(string[] args, string arg)
        {
            return args?.Any(a => a?.ToLowerInvariant() == arg?.ToLowerInvariant()) == true;
        }

        public static bool HasArgument(string[] args, string arg, out string value)
        {
            value = "";

            for (int i = 0; i < args.Length; i++)
            {
#pragma warning disable
                if (args[i].ToLowerInvariant() == arg.ToLowerInvariant())
#pragma warning restore
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        value = args[i + 1];
                        return true;
                    }
                }
            }

            return false;
        }

        public static T GetRegistryValue<T>(string path, string value, object defaultValue = null)
        {
            try
            {
                return (T)Registry.GetValue(path, value, defaultValue);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return (T)defaultValue;
            }
        }

        public static bool GetProcessRunning(string appName)
        {
            return GetProcess(appName)?.ProcessName?.Equals(appName, StringComparison.InvariantCultureIgnoreCase) == true;
        }

        public static Process GetProcess(string appName)
        {
            return Process.GetProcessesByName(appName)?.FirstOrDefault();
        }

        public static void KillProcess(string appName)
        {
            Process proc = GetProcess(appName);
            proc?.Kill();
        }

        public static void CloseProcess(string appName)
        {
            Process proc = GetProcess(appName);
            proc?.Close();
        }

        public static void StartProcess(string absolutePath, string workDirectory = null, string args = null, bool useShell = true)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return;

            if (!absolutePath.StartsWith("http") && (!File.Exists(absolutePath) || workDirectory != null && !Directory.Exists(workDirectory)))
            {
                Logger.Warning($"The Path '{absolutePath}' does not exist! (WorkDir was: '{workDirectory}')");
                return;
            }

            var pProcess = new Process();
            pProcess.StartInfo.FileName = absolutePath;
            pProcess.StartInfo.UseShellExecute = useShell;
            pProcess.StartInfo.WorkingDirectory = workDirectory ?? Directory.GetCurrentDirectory();
            if (args != null)
                pProcess.StartInfo.Arguments = args;
            pProcess.Start();
        }

        public static bool RunCommand(string command, out string strOutput, int exitCode = 0)
        {
            var pProcess = new Process();
            pProcess.StartInfo.FileName = "cmd.exe";
            pProcess.StartInfo.Arguments = "/C" + command;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.StartInfo.RedirectStandardOutput = true;
            Logger.Debug($"Running Command: cmd.exe /C {command}");
            pProcess.Start();
            strOutput = pProcess?.StandardOutput?.ReadToEnd() ?? "";
            pProcess.WaitForExit();

            Logger.Debug($"Command exited with Code '{pProcess.ExitCode}'");
            return pProcess.ExitCode == exitCode;
        }

        public static bool CreateLink(string name, string path, string description, Environment.SpecialFolder folder = Environment.SpecialFolder.Desktop)
        {
            bool result;
            IShellLink link = (IShellLink)new ShellLink();

            link.SetDescription(description);
            link.SetPath(path);

            IPersistFile file = (IPersistFile)link;
            string desktopPath = Environment.GetFolderPath(folder);
            file.Save(Path.Combine(desktopPath, $"{name}.lnk"), false);
            result = true;

            return result;
        }

        public static bool IsEnter(KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
                return true;
            else
                return false;
        }

        public const uint WM_CLOSE = 0x0010;
        public const uint WM_DESTROY = 0x0002;
        public const uint WM_QUIT = 0x0012;

#pragma warning disable
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        private static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, UIntPtr Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostThreadMessage(UIntPtr threadId, UIntPtr msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
#pragma warning restore
        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        public static int SendWindowMessage(IntPtr hwnd, UIntPtr msg, IntPtr wParam, IntPtr lParam)
        {
            return SendMessage(hwnd, msg, wParam, lParam).ToInt32();
        }

        public static bool SendThreadMessage(UIntPtr threadId, UIntPtr msg, IntPtr wParam, IntPtr lParam)
        {
            return PostThreadMessage(threadId, msg, wParam, lParam);
        }

        public static void SetForegroundWindow(string title)
        {
            SetForegroundWindow(FindWindowByCaption(IntPtr.Zero, title));
        }

        public static int FindWindowByCaption(string title)
        {
            return FindWindowByCaption(IntPtr.Zero, title).ToInt32();
        }
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out int pfd, int fFlags);
        void GetIDList(out int ppidl);
        void SetIDList(int pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(int hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
