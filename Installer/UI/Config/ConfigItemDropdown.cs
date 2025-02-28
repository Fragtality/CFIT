using CFIT.Installer.Product;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CFIT.Installer.UI.Config
{
    public class ConfigItemDropdown : ConfigItem
    {
        public virtual string Text { get; set; } = "";
        public virtual StackPanel Panel { get { return Element as StackPanel; } }
        public virtual ComboBox DropDown { get; protected set; }
        public virtual Dictionary<int, string> Options { get; protected set; }

        public ConfigItemDropdown(string name, string text, Dictionary<int, string> options, string key, ConfigBase config) : base(name, key, config)
        {
            Text = text;
            Options = new Dictionary<int, string>(options);
        }

        public override UIElement CreateElement()
        {
            if (Element == null)
            {
                Element = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                if (!string.IsNullOrWhiteSpace(Text))
                    Panel.Children.Add(new TextBlock() { Text = this.Text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,8,0) });

                DropDown = new ComboBox()
                {
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Center,
                };
                foreach (var option in Options)
                {
                    var item = new ComboBoxItem()
                    {
                        Tag = option.Key,
                        Content = option.Value,
                    };
                    DropDown.Items.Add(item);
                }                

                SetValueElement();
                DropDown.SelectionChanged += SetValueConfig;
                Panel.Children.Add(DropDown);
            }

            return Element;
        }

        protected override void SetValueElement()
        {
            DropDown.SelectedIndex = Config.GetOption<int>(Key);
        }

        protected override void SetValueConfig(object sender, RoutedEventArgs e)
        {
            if (DropDown?.SelectedIndex >= 0)
                Config.SetOption(Key, DropDown.SelectedIndex);
        }
    }
}
