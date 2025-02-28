using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CFIT.AppTools
{
    public static class Gui
    {
        public static void SetArrowWidth(this ComboBox comboBox, int width)
        {
            var path = comboBox?.GetArrowPath();
            if (path != null)
                path.Width = width;
        }

        public static System.Windows.Shapes.Path GetArrowPath(this ComboBox comboBox)
        {
            return FindVisualChild<System.Windows.Shapes.Path>(comboBox);
        }

        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    var child = VisualTreeHelper.GetChild(obj, i);
                    if (child is T)
                    {
                        return (T)child;
                    }

                    T childItem = FindVisualChild<T>(child);
                    if (childItem != null) return childItem;
                }
            }
            return null;
        }

        //VS: Embedded Resource
        public static void SetImageSourceFromResourceManifest(this Image image, string icon, string prefix = "", Assembly assembly = null, string extension = ".png")
        {
            image.Source = GetBitmapImageFromResourceManifest(icon, prefix, assembly, extension);
        }

        public static BitmapImage GetBitmapImageFromResourceManifest(string icon, string prefix = "", Assembly assembly = null, string extension = ".png")
        {
            try
            {
                if (assembly == null)
                    assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream($"{prefix}{icon}{extension}"))
                {
                    if (stream == null)
                        return null;
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    return ToImage(buffer);
                }
            }
            catch
            {
                return null;
            }
        }

        //VS: Resource
        public static void SetImageSourceFromPackUri(this Image image, Uri uri)
        {
            image.Source = new BitmapImage(uri);
        }

        public static void SetImageSourceFromPackUri(this Image image, string icon, string prefix = "/", string extension = ".png")
        {
            image.Source = GetBitmapImageFromPackUri(icon, prefix, extension);
        }

        public static BitmapImage GetBitmapImageFromPackUri(string icon, string prefix = "/", string extension = ".png")
        {
            return new BitmapImage(new Uri($"pack://application:,,,{prefix}{icon}{extension}"));
        }

        public static BitmapImage ToImage(byte[] array)
        {
            using (var ms = new MemoryStream(array))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }

        public static void SetButton(Button button, bool visible, bool hitTest = true, string caption = null, SolidColorBrush brush = null, BitmapImage icon = null, string tooltip = null)
        {
            if (button == null)
                return;

            button.IsHitTestVisible = hitTest;

            if (!visible)
                button.Visibility = Visibility.Collapsed;
            else
                button.Visibility = Visibility.Visible;

            if (tooltip != null)
                button.ToolTip = tooltip;


            if (!(button.Content is StackPanel panel) || panel?.Children?.Count != 2)
                return;

            if (caption != null && panel?.Children[0] is TextBlock captionLabel)
                captionLabel.Text = caption;

            if (brush != null && panel?.Children[0] is TextBlock brushLabel)
                brushLabel.Foreground = brush;

            if (icon != null && panel?.Children[1] is Image image)
            {
                image.Source = icon;
            }
        }
    }
}
