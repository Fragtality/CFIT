using CFIT.Installer.Product;
using System.Windows;
using System.Windows.Controls;

namespace CFIT.Installer.UI.Config
{
    public class ConfigItemCheckbox : ConfigItem
    {
        public virtual string Text { get; set; } = "";
        public virtual CheckBox CheckBox { get { return Element as CheckBox; } }

        public ConfigItemCheckbox(string name, string text, string key, ConfigBase config) : base(name, key, config)
        {
            Text = text;
        }

        public override UIElement CreateElement()
        {
            if (Element == null)
            {
                Element = new CheckBox()
                {
                    Content = Text
                };
                SetValueElement();
                CheckBox.Click += SetValueConfig;
            }

            return Element;
        }

        protected override void SetValueConfig(object sender, RoutedEventArgs e)
        {
            Config.SetOption(Key, CheckBox.IsChecked == true);
        }

        protected override void SetValueElement()
        {
            CheckBox.IsChecked = Config.GetOption<bool>(Key);
        }
    }
}
