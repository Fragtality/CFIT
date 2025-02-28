using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace CFIT.Installer.LibFunc
{
    public static class FuncZip
    {
        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite, List<string> exclusions = null)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }

            if (exclusions == null)
                exclusions = new List<string>();

            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                string directory = Path.GetDirectoryName(completeFileName);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (file.Name != "" && !exclusions.Contains(file.Name))
                {
                    file.ExtractToFile(completeFileName, overwrite);
                }
            }
        }

        public static bool ExtractZipStream(string extractDir, Stream archiveStream, string unblockDir, bool overwrite, List<string> exclusions = null)
        {
            try
            {
                Logger.Debug($"Extracting Stream to '{extractDir}' ...");
                ZipArchive archive = new ZipArchive(archiveStream);
                archive.ExtractToDirectory(extractDir, overwrite, exclusions);
                archiveStream.Close();

                if (!string.IsNullOrEmpty(unblockDir))
                {
                    Logger.Debug($"Running Unblock-File on '{unblockDir}'");
                    Sys.RunCommand($"powershell -WindowStyle Hidden -Command \"dir -Path {unblockDir} -Recurse | Unblock-File\"", out _);
                }

                return true;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
                return false;
            }
        }

        public static bool ExtractZipFile(string extractDir, string zipFile, string unblockDir = null, bool overwrite = false, List<string> exclusions = null)
        {
            try
            {
                using (Stream stream = new FileStream(zipFile, FileMode.Open))
                {
                    ExtractZipStream(extractDir, stream, unblockDir, overwrite, exclusions);
                    stream.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
                return false;
            }
        }
    }
}
