using System;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.Installer.Tasks
{
    public static class TaskWaiter
    {
        public static async Task CountdownWaiter(TaskModel model, string message, int seconds, CancellationToken token, TaskState stateEnded = TaskState.ACTIVE, Action onWaitEnded = null)
        {
            for (int i = seconds; i > 0; i--)
            {
                if (message.Contains("{0}"))
                    model.ReplaceLastMessage(string.Format(message, i));
                else
                    model.ReplaceLastMessage($"{message} {i}s");
                await Task.Delay(1000);
                if (token.IsCancellationRequested)
                    return;
            }
            model.RemoveLastMessage();
            onWaitEnded?.Invoke();

            model.State = stateEnded;
        }

        public static async Task<bool> TimeoutWaiter(TaskModel model, string message, int maxSeconds, Func<bool> waitCondition, CancellationToken token, Action workAction = null)
        {
            int i = 1;
            workAction?.Invoke();
            bool waitResult = waitCondition?.Invoke() == true;
            while (waitResult && i <= maxSeconds && !token.IsCancellationRequested)
            {
                if (message.Contains("{0}") && message.Contains("{1}"))
                    model.ReplaceLastMessage(string.Format(message, i, maxSeconds));
                else if (message.Contains("{0}"))
                    model.ReplaceLastMessage(string.Format(message, i));
                else
                    model.ReplaceLastMessage($"{message} {i}/{maxSeconds}s");
                await Task.Delay(1000);
                i++;
                workAction?.Invoke();
                waitResult = waitCondition?.Invoke() == true;
            }
            model.RemoveLastMessage();

            return !waitResult;
        }
    }
}
