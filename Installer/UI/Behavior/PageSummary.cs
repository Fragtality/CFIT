using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using CFIT.Installer.UI.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CFIT.Installer.UI.Behavior
{
    public class PageSummary : IPageBehavior
    {
        public PageSummary() : base()
        {

        }

        public override void Activate(InstallerWindow window)
        {
            base.Activate(window);
            if (BaseBehavior?.FocusInstallerOnSummary == true)
            {
                Sys.SetForegroundWindow(InstallerWindow.WindowTitle);
            }
        }

        protected override void SetHeader()
        {
            string mode = "Installation";
            if (BaseConfig?.Mode == SetupMode.UPDATE)
                mode = "Update";
            if (BaseConfig?.Mode == SetupMode.REMOVE)
                mode = "Removal";

            TextBlock header = new TextBlock()
            {
                Text = $"{mode} ",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
            };

            Run result;
            if (BaseWorker?.IsSuccess == true)
            { 
                result = new Run()
                {
                    Text = "successful!",
                    FontSize = 14,
                    Foreground = Brushes.Green,
                    FontWeight = FontWeights.Bold,
                };
            }
            else
            {
                result = new Run()
                {
                    Text = "FAILED!",
                    FontSize = 14,
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold,
                };
            }
            header.Inlines.Add(result);

            Window?.PanelHeader?.Children?.Add(header);
        }

        protected override void SetContent()
        {
            var panel = new StackPanel()
            {
                Orientation = Orientation.Vertical
            };
            ContentRef = panel;

            if (BaseBehavior?.ShowTasksInSummary == true)
            {
                var list = TaskStore.List;
                foreach (var task in list)
                {
                    if (task.DisplayInSummary)
                        panel.Children.Add(new TaskView(task, true));
                }
            }

            if (BaseBehavior?.ShowInstallPathInSummary == true && BaseWorker?.IsSuccess == true && BaseConfig?.Mode == SetupMode.INSTALL)
            {
                var block = new TextBlock
                {
                    Text = $"{BaseConfig?.ProductName} was installed to:\r\n{BaseConfig?.ProductPath}",
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 448,
                    Margin = new Thickness(0,32,0,0),
                };
                panel.Children.Add(block);
            }

            if (BaseWorker?.IsSuccess == false)
            {
                string text = $"The Installer ran into an Error!\r\n\r\nUse the 'Get Logs' Button to view the Log ({Logger.FileName}).\r\nYou need to provide the Log-File to get Support!";
                if (BaseBehavior?.ShowTasksInSummary == false)
                    text = $"The Installer ran into an Error:\r\n{TaskStore.CurrentTask?.ErrorMessage}\r\n\r\n\r\nUse the 'Get Logs' Button to view the Log ({Logger.FileName}).\r\nYou need to provide the Log-File to get Support!";

                var block = new TextBlock
                {
                    Text = text,
                    FontSize = 12,
                    Margin = new Thickness(0, 32, 0, 0),
                    FontWeight = FontWeights.DemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 448,
                    MinHeight = 64
                };
                panel.Children.Add(block);
            }
        }

        protected override void SetFooter()
        {
            
        }

        protected override void SetButtons()
        {
            if (BaseWorker?.IsSuccess == true)
                Gui.SetButton(Window?.ButtonLeft, false, false, "");
            else
                Gui.SetButton(Window?.ButtonLeft, true, true, "Get Logs", SystemColors.ControlTextBrush, IconLoader.Instance.LoadIcon("box-arrow-in-right"), "");

            if (BaseWorker?.IsSuccess == true)
                Gui.SetButton(Window?.ButtonRight, true, true, "Close", SystemColors.ControlTextBrush, IconLoader.Instance.LoadIcon("check-square"), "Close Installer");
            else
                Gui.SetButton(Window?.ButtonRight, true, true, "Close", SystemColors.ControlTextBrush, IconLoader.Instance.LoadIcon("box-arrow-in-right"), "Close Installer");
        }

        protected override void SetActions()
        {
            if (BaseWorker?.IsSuccess == true)
            {
                Window.ActionLeft = null;
            }
            else
            {
                Window.ActionLeft = (w) =>
                {
                    LogAction();
                };
            }

            Window.ActionRight = (w) =>
            {
                Gui.SetButton(w.ButtonRight, true, false);
                w.SetPage();
                w.Close();
            };
        }

        protected virtual void LogAction()
        {
            string workDir = Directory.GetCurrentDirectory();
            Nav.OpenFolder(workDir);
            Sys.StartProcess($@"{workDir}\{Logger.FileName}");
            Gui.SetButton(Window?.ButtonLeft, true, false);
        }
    }
}
