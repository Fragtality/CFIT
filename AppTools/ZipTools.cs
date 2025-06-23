#if NET8_0_OR_GREATER
using CFIT.AppLogger;
using System;
using System.IO;
using System.IO.Compression;
#endif

namespace CFIT.AppTools
{
    public static class ZipTools
    {
#if NET8_0_OR_GREATER
        public static void ExtractArchiveDirectory(this ZipArchive archive, string directoryName, string destinationPath, bool recursive = true)
        {
            string extractPath = Path.Join(destinationPath, directoryName);
            Logger.Debug($"Extracting Directory {directoryName} from Archive to {extractPath} (recursive {recursive})");
            if (Directory.Exists(extractPath))
            {
                Logger.Debug($"Delete existing Path {extractPath}");
                Directory.Delete(extractPath, true);
            }

            bool isDirectory;
            foreach (var entry in archive.Entries)
            {
                if (!entry.FullName.StartsWith($"{directoryName}/",StringComparison.InvariantCultureIgnoreCase))
                    continue;

                isDirectory = entry.FullName.EndsWith('/');
                if (isDirectory && !recursive)
                    continue;

                extractPath = Path.Join(destinationPath, entry.FullName);
                if (isDirectory)
                    Directory.CreateDirectory(extractPath);
                else
                {
                    string temp = entry.FullName.Replace($"{directoryName}/", "");
                    if (temp.EndsWith('/') && !recursive)
                        continue;

                    entry.ExtractToFile(extractPath);
                }
            }
        }
#endif
    }
}
