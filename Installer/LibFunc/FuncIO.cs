using CFIT.AppLogger;
using CFIT.Installer.Tasks;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.Installer.LibFunc
{
    public static class FuncIO
    {
        public static async Task<string> DownloadFile(CancellationToken token, string url, string file, HttpClient httpClient = null, int checkInterval = 250, string workdir = "")
        {
            string result = null;
            try
            {
                if (string.IsNullOrWhiteSpace(workdir))
                    workdir = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);

                file = $@"{workdir}\{file}";

                if (File.Exists(file))
                    File.Delete(file);

                if (httpClient == null)
                {
                    httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0");
                }
                
                Logger.Debug($"Starting Download of {url} to byte array ...");
                var task = httpClient.GetByteArrayAsync(url);
                while (!task.IsCompleted && !task.IsFaulted && !token.IsCancellationRequested)
                    await Task.Delay(checkInterval, token);

                int? length = task?.Result?.Length;
                if (task.IsCompleted && !task.IsFaulted && length > 1 && !token.IsCancellationRequested)
                {
                    Logger.Debug($"Download finished. Saving byte array to {file} ...");
                    File.WriteAllBytes(file, task.Result);
                    if (File.Exists(file) && (new FileInfo(file))?.Length > 1)
                        result = file;
                }
                else
                    Logger.Warning($"Download failed! (failed: {task.IsFaulted} | len: {length})");
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return result;
        }

        public static void DeleteDirectory(string path, bool recursive = true, bool create = false)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive);
                if (create)
                    Directory.CreateDirectory(path);
            }
            else if (create)
                Directory.CreateDirectory(path);
        }

        public static void DeleteFile(string path, bool create = false)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                if (create)
                    File.Create(path).Close();
            }
            else if (create)
                File.Create(path).Close();
        }
    }
}
