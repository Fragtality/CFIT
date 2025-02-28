
using CFIT.AppLogger;
using CFIT.Installer.Tasks;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CFIT.Installer.UI.Tasks
{
    public class TaskViewPanel : StackPanel
    {
        public int TaskLimit { get; set; } = -1;
        protected List<TaskView> TaskViewList { get; set; } = new List<TaskView>();
        protected DispatcherTimer TimerRefreshList { get; set; }
        protected int LastCount { get; set; } = 0;

        public TaskViewPanel(int limit = -1) : base()
        {
            TaskLimit = limit;
            TimerRefreshList = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            TimerRefreshList.Tick += TimerRefreshListTick;
        }

        protected void TimerRefreshListTick(object sender, EventArgs e)
        {
            try
            {
                if (TaskStore.Count > LastCount)
                {
                    var list = TaskStore.List;
                    int index = LastCount;
                    while (index < list.Count)
                    {
                        Logger.Debug($"Adding Task '{list[index].Title}'");
                        AddTaskView(list[index]);
                        index++;
                    }
                }
                LastCount = TaskStore.Count;
                FilterView();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void FilterView()
        {
            TaskViewList.RemoveAll(v => v.Visibility == Visibility.Collapsed && v.Model.DisplayCompleted == false);

            while (TaskLimit != -1 && TaskViewList.Count > TaskLimit)
            {
                int i = 0;
                bool found = false;
                while (!found && i < TaskViewList.Count)
                {
                    if (TaskViewList[i]?.Model.DisplayPinned == false)
                        found = true;
                    else
                        i++;
                }
                
                TaskViewList[i].Disable(true);
                TaskViewList.RemoveAt(i);
            }
        }

        protected void AddTaskView(TaskModel task)
        {
            TaskView component = new TaskView(task);
            Children.Add(component);
            TaskViewList.Add(component);
        }

        protected void ClearList()
        {
            foreach (var taskView in TaskViewList)
                taskView.Disable(true);
            TaskViewList.Clear();
        }

        public void Activate()
        {
            ClearList();
            Children.Clear();
            TimerRefreshList.Start();
        }

        public void Deactivate(bool clearTaskList = true)
        {
            TimerRefreshList.Stop();

            if (clearTaskList)
            {
                ClearList();
                Children.Clear();
            }
        }
    }
}
