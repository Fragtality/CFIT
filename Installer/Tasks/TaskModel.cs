using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace CFIT.Installer.Tasks
{
    public enum TaskState
    {
        ACTIVE = 1,
        WAITING = 2,
        ERROR = 3,
        COMPLETED = 4
    }

    public class TaskModel
    {
        public virtual string Title { get; set; } = "";
        public virtual List<TaskMessage> ListMessages { get; } = new List<TaskMessage>();
        public virtual TaskMessage LastMessage { get { return ListMessages?.LastOrDefault(); } }
        public virtual List<TaskLink> Links { get; } = new List<TaskLink>();
        public virtual Orientation LinkOrientation { get; set; } = Orientation.Vertical;
        public virtual bool DisableAllLinksOnClick { get; set; } = true;
        public Enum LinkResponse { get; set; }
        public virtual bool ErrorLogged { get { return !string.IsNullOrWhiteSpace(ErrorMessage); } }
        public virtual string ErrorMessage { get; protected set; } = "";

        public virtual TaskState State { get; set; } = TaskState.ACTIVE;
        public virtual bool IsCompleted { get; set; } = false;
        public virtual bool IsSuccess { get; set; } = false;

        public virtual bool DisplayInSummary { get; set; } = false;
        public virtual bool DisplayPinned { get; set; } = false;
        public virtual bool DisplayCompleted { get; set; } = true;

        public TaskModel(string title, string message = "")
        {
            Title = title;
            if (!string.IsNullOrEmpty(message))
                ListMessages.Add(new TaskMessage(message));
            Logger.Debug($"Created Task '{Title}'");
        }

        public virtual TaskLink AddLink(string title, Action callback)
        {
            var link = new TaskLink(this, title, callback);
            Links.Add(link);

            return link;
        }

        public virtual TaskLink AddLink(string title, string url, string args = "")
        {
            var link = new TaskLink(this, title, url, args);
            Links.Add(link);

            return link;
        }

        public virtual string Message
        { 
            get { return ListMessages?.LastOrDefault()?.Text; }
            set { ListMessages.Add(new TaskMessage(value)); Logger.Information(value); }
        }

        public virtual TaskMessage AddMessage(string text, bool showCompleted = false, bool replace = false, bool noLog = false, FontWeight? weight = null, TextDecorationCollection decorations = null, FontStyle? style = null, bool newline = true)
        {
            var msg = new TaskMessage(text)
            {
                FontWeight = weight ?? FontWeights.Normal,
                Decorations = decorations,
                Style = style ?? FontStyles.Normal,
                Newline = newline,
                ShowCompleted = showCompleted,
            };

            AddMessage(msg, replace, noLog);

            return msg;
        }

        public virtual void AddMessage(TaskMessage message, bool replace, bool noLog)
        {
            if (replace)
                RemoveLastMessage();
            ListMessages.Add(message);

            if (!noLog)
                Logger.Information(message.Text);
        }

        public virtual void RemoveLastMessage()
        {
            if (ListMessages.Count > 0)
                ListMessages.RemoveAt(ListMessages.Count - 1);
        }

        public virtual void ReplaceLastMessage(TaskMessage message)
        {
            RemoveLastMessage();
            AddMessage(message, false, false);
        }

        public virtual void ReplaceLastMessage(string message)
        {
            ReplaceLastMessage(new TaskMessage(message));
        }

        public virtual void SetState(TaskMessage message, TaskState state = (TaskState)(-1), bool replace = false)
        {
            AddMessage(message, replace, false);

            if (state != (TaskState)(-1))
                State = state;
            if (state == TaskState.ERROR)
                SetErrorMessage(message.Text);
        }

        public virtual void SetState(string message, TaskState state = (TaskState)(-1), bool replace = false)
        {
            SetState(new TaskMessage(message), state, replace);
        }

        public virtual void SetSuccess(TaskMessage message, bool replace = false)
        {
            AddMessage(message, replace, false);

            State = TaskState.COMPLETED;
        }

        public virtual void SetSuccess(string message, bool replace = false, FontWeight? fontWeight = null)
        {
            SetSuccess(new TaskMessage(message, true, fontWeight), replace);
        }

        protected virtual void SetErrorMessage(string message)
        {
            ErrorMessage = message;
        }

        public virtual void SetError(Exception ex, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            Logger.LogException(ex, classFile, classMethod);
            SetErrorMessage($"{ex.GetType()}: {ex.Message}");
            AddMessage(ErrorMessage, true, false, true, FontWeights.DemiBold);
            DisplayInSummary = true;
            State = TaskState.ERROR;
        }

        public virtual void SetError(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            Logger.Error(message, classFile, classMethod);
            SetErrorMessage(message);
            AddMessage(message, true, false, true, FontWeights.DemiBold);
            DisplayInSummary = true;
            State = TaskState.ERROR;
        }
    }
}
