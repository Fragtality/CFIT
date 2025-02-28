using CFIT.Installer.Product;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CFIT.Installer.UI.Config
{
    public class ConfigItemRadio : ConfigItem
    {
        public virtual StackPanel RadioPanel { get { return Element as StackPanel; } }
        public virtual Dictionary<int, RadioButton> RadioButtons { get; } = new Dictionary<int, RadioButton>();
        public virtual Dictionary<int, string> RadioOptions { get; protected set; }

        public ConfigItemRadio(string name, Dictionary<int, string> options, string key, ConfigBase config) : base(name, key, config)
        {
            RadioOptions = new Dictionary<int, string>(options);
        }

        public override UIElement CreateElement()
        {
            if (Element == null)
            {
                Element = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                };

                foreach (var option in RadioOptions)
                {
                    var radio = new RadioButton()
                    {
                        Content = new TextBlock() { Text = option.Value, VerticalAlignment = VerticalAlignment.Center },
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(2)
                    };
                    radio.Click += SetValueConfig;
                    RadioButtons.Add(option.Key, radio);
                    RadioPanel.Children.Add(radio);
                }

                SetValueElement();
            }

            return Element;
        }

        protected override void SetValueConfig(object sender, RoutedEventArgs e)
        {
            foreach (var option in RadioButtons)
            {
                if (option.Value == sender as RadioButton)
                    Config.SetOption(Key, option.Key);
            }
        }

        protected override void SetValueElement()
        {
            int value = Config.GetOption<int>(Key);
            foreach (var option in RadioButtons)
                option.Value.IsChecked = option.Key == value;
        }
    }
}
