using System.Threading;

namespace CFIT.AppTools
{
    public static class MutexExt
    {
        public static void TryReleaseMutex(this Mutex mutex)
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch { }
        }

        public static void TryWaitOne(this Mutex mutex)
        {
            try
            {
                mutex.WaitOne();
            }
            catch (AbandonedMutexException) { }
        }
    }
}
