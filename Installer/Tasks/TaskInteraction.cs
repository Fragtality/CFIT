using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.Installer.Tasks
{
    public enum InteractionResponse
    {
        UNKNOWN = 0,
        YES = 1,
        NO = 2,
        RETRY = 3,
        CANCEL = 4,
    }

    public class TaskInteraction
    {
        public virtual TaskModel Model { get; protected set; }
        public virtual List<TaskLink> Links { get { return Model?.Links; } }

        public TaskInteraction(TaskModel model)
        {
            Model = model;
        }

        public virtual void AddInteraction(string title, Enum response, Action callback = null)
        {
            if (callback != null)
                Model.AddLink(title, callback);
            else
                Model.AddLink(title, () => { });

            Model.Links.LastOrDefault().LinkResponse = response;
        }

        public virtual void AddInteraction(string title, Enum response, string url, string args = "")
        {
            Model.AddLink(title, url, args);
            Model.Links.LastOrDefault().LinkResponse = response;
        }

        public virtual async Task<Enum> WaitOnResponse(CancellationToken token, int interval = 150)
        {
            Model.State = TaskState.WAITING;
            while (!token.IsCancellationRequested && !Links.Any(l => l.WasNavigated))
            {
                await Task.Delay(interval, token);
            }
            Model.State = TaskState.ACTIVE;

            return Model.LinkResponse;
        }

        public virtual async Task<bool> WaitOnResponse(CancellationToken token, Enum response, int interval = 150)
        {
            return (await WaitOnResponse(token)).CompareTo(response) == 0;
        }
    }
}
