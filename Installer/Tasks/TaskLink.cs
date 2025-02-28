using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Windows.Navigation;

namespace CFIT.Installer.Tasks
{
    public class TaskLink
    {
        public virtual TaskModel Model { get; protected set; }
        public virtual string LinkTitle { get; set; } = "";
        public virtual string LinkUrl { get; set; } = "";
        public virtual string LinkArg { get; set; } = "";
        public virtual Enum LinkResponse { get; set; }
        public virtual Action LinkCallback { get; protected set; } = null;
        public virtual bool IsLinkCallback { get { return LinkUrl == CallbackUrl && LinkCallback != null; } }
        protected static string CallbackUrl { get; } = "action://callback";
        public virtual Action ClickedCallback { get; set; } = null;

        public virtual TaskState StateOnLinkClicked { get; set; } = TaskState.COMPLETED;
        public virtual bool WasNavigated { get; set; } = false;
        public virtual bool DisableLinkOnClick { get; set; } = true;

        public virtual bool LinkStyleBold { get; set; } = true;
        public virtual int LinkFontSize { get; set; } = -1;

        public TaskLink(TaskModel model, string title, string url, string args = "", Action callback = null)
        {
            SetLink(model, title, url, args, callback);
        }

        public TaskLink(TaskModel model, string title, Action callback)
        {
            SetLink(model, title, callback);
        }

        public virtual void SetLink(TaskModel model, string title, string url, string args = "", Action callback = null)
        {
            Model = model;
            LinkTitle = title;
            LinkUrl = url;
            LinkArg = args;
            LinkCallback = callback;
        }

        public virtual void SetLink(TaskModel model, string title, Action callback)
        {
            Model = model;
            LinkTitle = title;
            LinkUrl = CallbackUrl;
            LinkArg = "";
            LinkCallback = callback;
        }

        public virtual void RequestNavigateHandler(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Logger.Debug($"Handling Link '{LinkTitle}' ...");
                if (IsLinkCallback)
                    LinkCallback?.Invoke();
                else if (LinkArg == null)
                    Nav.OpenUri(sender, e);
                else
                    Nav.OpenUriArgs(LinkUrl, LinkArg);

                if (StateOnLinkClicked != Model.State)
                    Model.State = StateOnLinkClicked;
                
                Model.LinkResponse = LinkResponse;
                WasNavigated = true;
                e.Handled = true;
                Logger.Information($"Link '{LinkTitle}' was handled");
                ClickedCallback?.Invoke();
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }
        }
    }
}
