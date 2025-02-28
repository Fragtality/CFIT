using CFIT.Installer.Product;
using System.Windows;
using System.Windows.Media;

namespace CFIT.Installer.UI.Config
{
    public abstract class ConfigItem
    {
        public virtual ConfigBase Config { get; set; }
        public virtual string Name { get; set; }
        public virtual string Key { get; set; }
        public virtual Brush BorderBrush { get; set; } = Brushes.DarkGray;
        public virtual Thickness BorderThickness { get; set; } = new Thickness(1);
        public virtual string Tooltip { get; set; } = "";
        public virtual string Description { get; set; } = "";
        public virtual UIElement Element { get; protected set; }
        public abstract UIElement CreateElement();
        protected abstract void SetValueElement();
        protected abstract void SetValueConfig(object sender, RoutedEventArgs e);

        public ConfigItem(string name, string key, ConfigBase config)
        {
            Name = name;
            Key = key;
            Config = config;
        }

        public virtual void ClearElement()
        {
            Element = null;
        }
    }
}
