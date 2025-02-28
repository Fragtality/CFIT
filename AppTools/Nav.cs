using CFIT.AppLogger;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Navigation;

namespace CFIT.AppTools
{
    public static class Nav
    {
        public static void OpenFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Logger.Warning($"The Path '{path}' does not exist");
                return;
            }
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        public static void OpenUriArgs(string url, string args, string directory = null)
        {
            Sys.StartProcess(url, directory, args);
        }

        public static void OpenUri(object sender, RequestNavigateEventArgs e)
        {
            OpenUri(sender, e);
        }

#pragma warning disable IDE0060
        public static void OpenUri(object sender, RequestNavigateEventArgs e, string directory = null)
#pragma warning restore IDE0060
        {
            if (!e.Uri.ToString().Contains(".exe"))
                Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
            else
            {
                Sys.StartProcess(e.Uri.AbsolutePath, directory);
            }
            e.Handled = true;
        }

        public static void RequestNavigateHandler(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                e.Handled = true;
                Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
