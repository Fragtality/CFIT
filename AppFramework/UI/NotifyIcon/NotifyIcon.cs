using CFIT.AppTools;
using H.NotifyIcon;
using System;
using System.Windows.Controls;

namespace CFIT.AppFramework.UI.NotifyIcon
{
    public class NotifyIcon(ISimApp simApp, Type modelType)
    {
        public virtual NotifyIconViewModel Model { get; } = modelType.CreateInstance<NotifyIconViewModel, ISimApp>(simApp);

        public virtual TaskbarIcon TaskbarIcon { get; protected set; }

        public virtual void Initialize()
        {
            Model.Initialize();
            TaskbarIcon = CreateTaskbarIcon();
        }

        public virtual void SetIconNormal()
        {
            TaskbarIcon.Icon = Model.GetIconNormal();
        }

        public virtual void SetIconUpdate()
        {
            TaskbarIcon.Icon = Model.GetIconUpdate();
        }

        protected virtual TaskbarIcon CreateTaskbarIcon()
        {
            return new TaskbarIcon()
            {
                Icon = Model.AppIcon,
                ContextMenu = CreateMenu(),
                ToolTipText = Model.TextToolTip,
                NoLeftClickDelay = Model.NoLeftClickDelay,
                LeftClickCommand = Model.CommandLeftClick,
                DataContext = Model
            };
        }

        protected virtual ContextMenu CreateMenu()
        {
            var contextMenu = new ContextMenu();
            contextMenu.Items.Add(CreateMainItem());

            CreateMenuItems(contextMenu);

            CreateLastItem(contextMenu);
            return contextMenu;
        }

        protected virtual MenuItem CreateMainItem()
        {
            return new MenuItem() { Header = Model.TextToggleWindow, Command = Model.CommandToggleWindow };
        }

        protected virtual void CreateMenuItems(ContextMenu contextMenu)
        {
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(new MenuItem() { Header = Model.TextLogDir, Command = Model.CommandLogDir });

            foreach (var entry in Model.Items)
            {
                if (entry.Item1 == null)
                    contextMenu.Items.Add(new Separator());
                else
                    contextMenu.Items.Add(new MenuItem() { Header = entry.Item1, Command = entry.Item2 });
            }
        }

        protected virtual void CreateLastItem(ContextMenu contextMenu)
        {
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(new MenuItem() { Header = Model.TextExitApp, Command = Model.CommandExit });
        }

        public virtual void Show(bool enablesEfficiencyMode = false)
        {
            TaskbarIcon.ForceCreate(enablesEfficiencyMode: enablesEfficiencyMode);
        }

        public virtual void Dispose()
        {
            try { TaskbarIcon.Dispose(); TaskbarIcon = null; } catch { }
            try { Model.AppIcon.Dispose(); } catch { }
        }
    }
}
