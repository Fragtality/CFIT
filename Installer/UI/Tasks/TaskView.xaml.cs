using CFIT.Installer.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace CFIT.Installer.UI.Tasks
{
    public partial class TaskView : UserControl
    {
        protected static Brush BrushDefault { get; } = new SolidColorBrush(Colors.DarkGray);
        protected static Brush BrushError { get; } = new SolidColorBrush(Colors.Red);
        protected static Brush BrushCompleted { get; } = new SolidColorBrush(Colors.Green);
        protected static Brush BrushWaiting { get; } = new SolidColorBrush(Colors.Orange);
        protected static Brush BrushActive { get; } = new SolidColorBrush(Colors.DodgerBlue);

        public TaskModel Model { get; protected set; }
        protected bool FirstRefresh { get; set; } = true;
        protected bool IsSummary { get; set; } = false;

        protected Brush CurrentBrush { get; set; } = BrushActive;
        protected DispatcherTimer RefreshTimer { get; set; }
        protected string LastTitle { get; set; } = "";
        protected string LastMessageEntry { get; set; } = "";
        protected TaskState LastState { get; set; } = TaskState.ACTIVE;
        protected int LastLinkCount { get; set; } = 0;
        protected List<Tuple<TaskLink, Hyperlink>> LinkMappings { get; } = new List<Tuple<TaskLink, Hyperlink>>();

        public TaskView(TaskModel task, bool summary = false)
        {
            InitializeComponent();

            Model = task;
            IsSummary = summary;
            LastTitle = Model.Title;
            LastMessageEntry = Model.LastMessage?.Text;
            LastLinkCount = Model.Links.Count;
            LastState = Model.State;

            RefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            RefreshTimer.Tick += OnRefreshTimer;
            RefreshTimer.Start();
        }

        public void SetMinWidth(int minWidth)
        {
            Grid.MinWidth = minWidth;
        }

        protected void OnRefreshTimer(object sender, EventArgs e)
        {
            if (Model?.Links?.All(l => l.WasNavigated) == true && (Model?.IsCompleted == true || LinkMappings.Where(m => m.Item1.WasNavigated && m.Item2.IsEnabled).Any()))
            {
                UpdateTaskView(true);
                Disable(!IsSummary && Model?.DisplayCompleted == false);
            }
            else
            {
                if (FirstRefresh)
                    PanelLinks.Orientation = Model.LinkOrientation;

                UpdateTaskView(FirstRefresh);
                FirstRefresh = false;
            }            
        }

        public void Disable(bool collapse)
        {
            if (collapse)
                Visibility = Visibility.Collapsed;
            RefreshTimer?.Stop();
        }

        public void UpdateTaskView(bool force = false)
        {
            if (LastTitle != Model.Title || force)
                Title.Content = Model.Title;

            UpdateMessages(force);
            UpdateLinks(force);
            UpdateBorder(force);

            LastTitle = Model.Title;
            LastMessageEntry = Model.LastMessage?.Text;
            LastLinkCount = Model.Links.Count;
            LastState = Model.State;
        }

        protected void UpdateMessages(bool force)
        {
            if (LastMessageEntry != Model.LastMessage?.Text || force)
            {
                PanelMessages.Inlines.Clear();
                if (Model.IsCompleted)
                {
                    bool first = true;
                    foreach (var msg in Model.ListMessages)
                    {
                        if (msg.ShowCompleted)
                        {
                            PanelMessages.Inlines.Add(msg.CreateElement(first));
                            first = false;
                        }
                    }
                }
                else
                {
                    bool first = true;
                    foreach (var msg in Model.ListMessages)
                    {
                        PanelMessages.Inlines.Add(msg.CreateElement(first));
                        first = false;
                    }
                }
            }
        }

        protected TextBlock CreateLinkControl(TaskLink link)
        {
            var run = new Run($"{link.LinkTitle}");
            if (link.LinkFontSize != -1)
                run.FontSize = link.LinkFontSize;
            if (link.LinkStyleBold)
                run.FontWeight = FontWeights.DemiBold;

            var hyperlink = new Hyperlink(run)
            {
                NavigateUri = new Uri($"{link.LinkUrl}"),
                IsEnabled = !link.WasNavigated,
            };
            hyperlink.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(link.RequestNavigateHandler));
            LinkMappings.Add(new Tuple<TaskLink, Hyperlink>(link, hyperlink));

            var block = new TextBlock(hyperlink)
            {
                Margin = new Thickness(0),
                Padding = new Thickness(0,2,8,2)
            };
            return block;
        }

        protected void UpdateLinks(bool force)
        {
            if (LastLinkCount == 0 && Model.Links.Count == 0 && PanelLinks.Visibility == Visibility.Visible)
            {
                PanelLinks.Visibility = Visibility.Collapsed;
            }
            else if (LastLinkCount != Model.Links.Count || force)
            {
                if (LastLinkCount == 0)
                    PanelLinks.Visibility = Visibility.Visible;

                PanelLinks.Children.Clear();
                LinkMappings.Clear();
                foreach (var link in Model.Links)
                {
                    if (link.WasNavigated && IsSummary)
                        continue;

                    var textblock = CreateLinkControl(link);
                    PanelLinks.Children.Add(textblock);
                }
            }
            else
            {
                if (Model.DisableAllLinksOnClick && Model.Links.Any(l => l.WasNavigated) && Model.Links.Any(l => !l.WasNavigated))
                    foreach (var link in Model.Links)
                        link.WasNavigated = true;

                foreach (var mapping in LinkMappings)
                {
                    if (mapping.Item1.WasNavigated && mapping.Item1.DisableLinkOnClick && mapping.Item2.IsEnabled)
                        mapping.Item2.IsEnabled = false;
                }

            }    
        }

        protected void UpdateBorder(bool force)
        {
            if (LastState != Model.State || force)
            {
                if (Model.State == TaskState.ERROR)
                    CurrentBrush = BrushError;
                else if (Model.State == TaskState.COMPLETED)
                    CurrentBrush = BrushCompleted;
                else if (Model.State == TaskState.WAITING)
                    CurrentBrush = BrushWaiting;
                else if (Model.State == TaskState.ACTIVE)
                    CurrentBrush = BrushActive;
                else
                    CurrentBrush = BrushDefault;

                TaskBorder.BorderBrush = CurrentBrush;
            }
        }
    }
}
