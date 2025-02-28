using CFIT.AppLogger;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CFIT.Installer.UI.Config
{
    public class ConfigPanel : StackPanel
    {
        public List<ConfigItem> Items { get; protected set; }

        public ConfigPanel() : base()
        {
            
        }

        public virtual void Activate(List<ConfigItem> items)
        {
            Logger.Debug($"Items in: {items.Count}");
            Items = new List<ConfigItem>(items);
            BuildConfigItems();
        }

        public virtual void Deactivate()
        {
            foreach (var item in Items)
                item?.ClearElement();
            Items?.Clear();
            Children?.Clear();
        }

        protected virtual void BuildConfigItems()
        {
            foreach (var item in Items)
            {
                var panel = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                };
                item.CreateElement();
                if (!string.IsNullOrWhiteSpace(item.Tooltip) && item.Element is Control c)
                    c.ToolTip = item.Tooltip;
                panel.Children.Add(item.Element);

                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    var desc = new TextBlock()
                    {
                        Text = item.Description,
                        Margin = new Thickness(20, 12, 0, 0),
                        FontSize = 10,
                    };
                    panel.Children.Add(desc);
                }

                var box = new GroupBox()
                {
                    Header = new Label() { Content = item.Name, FontWeight = FontWeights.DemiBold },
                    Content = panel,
                    Margin = new Thickness(16,16,16,0),
                    Padding = new Thickness(8),
                    BorderBrush = item.BorderBrush,
                    BorderThickness = item.BorderThickness,
                };
                if (!string.IsNullOrWhiteSpace(item.Tooltip))
                    box.ToolTip = item.Tooltip;

                Children.Add(box);
            }
        }
    }
}
