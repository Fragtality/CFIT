using CFIT.AppLogger;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.AppTools
{
    public static class TaskTools
    {
        public static Task RunPool(Action action, CancellationToken? token = null, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            return Task.Run(() =>
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(TaskCanceledException))
                        Logger.LogException(ex, "", classFile, classMethod);
                }
            }, token ?? CancellationToken.None);
        }

        public static Task RunPool(Func<Task> action, CancellationToken? token = null, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            return Task.Run(async () =>
            {
                try
                {
                    if (action != null)
                    {
                        var task = action();
                        if (task != null)
                            await task;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(TaskCanceledException))
                        Logger.LogException(ex, "", classFile, classMethod);
                }
            }, token ?? CancellationToken.None);
        }

        public static Task RunDelayed(Action action, int delay, CancellationToken? token = null, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            return Task.Run(async () =>
            {
                try
                {
                    if (action != null)
                    {
                        await Task.Delay(delay, token ?? CancellationToken.None);
                        action?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(TaskCanceledException))
                        Logger.LogException(ex, "", classFile, classMethod);
                }
            }, token ?? CancellationToken.None);
        }

        public static Task RunDelayed(Func<Task> action, int delay, CancellationToken? token = null, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            return Task.Run(async () =>
            {
                try
                {
                    if (action != null)
                    {
                        await Task.Delay(delay, token ?? CancellationToken.None);
                        var task = action();
                        if (task != null)
                            await task;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(TaskCanceledException))
                        Logger.LogException(ex, "", classFile, classMethod);
                }
            }, token ?? CancellationToken.None);
        }

        public static void RunSync(this Task task)
        {
            task.GetAwaiter().GetResult();
        }

        public static T RunSync<T>(this Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }
    }

    public class DispatcherTimerAsync : IDisposable
    {
        public virtual TimeSpan Interval { get; set; }
        protected virtual CancellationTokenSource TokenSource { get; set; }
        public virtual bool IsEnabled { get; protected set; }
        public virtual bool IsTickRunning { get; protected set; }

        public event Func<Task> Tick;

        public DispatcherTimerAsync(TimeSpan interval)
        {
            Interval = interval;
        }

        public DispatcherTimerAsync(int interval = 500) : this(TimeSpan.FromMilliseconds(interval)) { }

        public virtual void Start()
        {
            if (IsEnabled)
                return;
            IsEnabled = true;

            TokenSource = new CancellationTokenSource();
            Task.Run(async () => await RunTimerLoop(TokenSource.Token));
        }

        public virtual void Stop()
        {
            TokenSource?.Cancel();
            IsEnabled = false;
        }

        protected virtual async Task RunTimerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!IsTickRunning)
                    {
                        IsTickRunning = true;
                        if (Tick != null)
                            await Tick?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(TaskCanceledException))
                        Logger.LogException(ex);
                    else
                        break;
                }
                finally
                {
                    IsTickRunning = false;
                }

                try
                {
                    if (!token.IsCancellationRequested)
                        await Task.Delay(Interval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            Stop();
            TokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
