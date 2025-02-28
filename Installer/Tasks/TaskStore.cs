using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CFIT.Installer.Tasks
{
    public static class TaskStore
    {
        private static ConcurrentQueue<TaskModel> Store { get; } = new ConcurrentQueue<TaskModel>();

        public static List<TaskModel> List { get { return new List<TaskModel>(Store?.ToArray()); } }
        public static int Count { get { return Store.Count; } }
        public static TaskModel CurrentTask { get; private set; } = null;

        public static TaskModel Add(string title, string message = "")
        {
            return Add(new TaskModel(title, message));
        }

        public static TaskModel Add(TaskModel task)
        {
            CurrentTask = task;
            Store.Enqueue(CurrentTask);

            return CurrentTask;
        }

        public static void Clear()
        {
            while (!Store.IsEmpty)
                Store.TryDequeue(out _);
            CurrentTask = null;
        }
    }
}
