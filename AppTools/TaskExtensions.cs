using CFIT.AppLogger;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.AppTools
{
    public static class TaskTools
    {
        public static Task RunLogged(Action action, CancellationToken? token = null, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            return Task.Run(() =>
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "", classFile, classMethod);
                }
            }, token ?? CancellationToken.None);
        }
    }
}
