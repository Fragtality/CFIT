using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CFIT.AppTools
{
    public static class Gui
    {
        public static void SetArrowWidth(this ComboBox comboBox, int width)
        {
            var path = comboBox?.GetArrowPath();
#if NET10_0_OR_GREATER
            path?.Width = width;
#else
            if (path != null)
                path.Width = width;
#endif
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
                    if (child is T t)
                    {
                        return t;
                    }

                    T childItem = FindVisualChild<T>(child);
                    if (childItem != null) return childItem;
                }
            }
            return null;
        }

        //VS: Embedded Resource
        public static void SetImageSourceFromResourceManifest(this System.Windows.Controls.Image image, string icon, string prefix = "", Assembly assembly = null, string extension = ".png")
        {
            image.Source = GetBitmapImageFromResourceManifest(icon, prefix, assembly, extension);
        }

        public static BitmapImage GetBitmapImageFromResourceManifest(string icon, string prefix = "", Assembly assembly = null, string extension = ".png")
        {
            try
            {
                if (assembly == null)
                    assembly = Assembly.GetExecutingAssembly();
#if NET10_0_OR_GREATER
                using var stream = assembly.GetManifestResourceStream($"{prefix}{icon}{extension}");
                if (stream == null)
                    return null;
                byte[] buffer = new byte[stream.Length];
                stream.ReadExactly(buffer, 0, buffer.Length);
                return ToImage(buffer);
#else
                using (var stream = assembly.GetManifestResourceStream($"{prefix}{icon}{extension}"))
                {
                    if (stream == null)
                        return null;
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    return ToImage(buffer);
                }
#endif
            }
            catch
            {
                return null;
            }
        }

        //VS: Resource
        public static void SetImageSourceFromPackUri(this System.Windows.Controls.Image image, Uri uri)
        {
            image.Source = new BitmapImage(uri);
        }

        public static void SetImageSourceFromPackUri(this System.Windows.Controls.Image image, string icon, string prefix = "/", string extension = ".png")
        {
            image.Source = GetBitmapImageFromPackUri(icon, prefix, extension);
        }

        public static BitmapImage GetBitmapImageFromPackUri(string icon, string prefix = "/", string extension = ".png")
        {
            return new BitmapImage(new Uri($"pack://application:,,,{prefix}{icon}{extension}"));
        }

        public static BitmapImage ToImage(byte[] array)
        {
#if NET10_0_OR_GREATER
            using var ms = new MemoryStream(array);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            return image;
#else
            using (var ms = new MemoryStream(array))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
#endif
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

#if NET10_0_OR_GREATER
            if (button.Content is not StackPanel panel || panel?.Children?.Count != 2)
                return;
#else
            if (!(button.Content is StackPanel panel) || panel?.Children?.Count != 2)
                return;
#endif

            if (caption != null && panel?.Children[0] is TextBlock captionLabel)
                captionLabel.Text = caption;

            if (brush != null && panel?.Children[0] is TextBlock brushLabel)
                brushLabel.Foreground = brush;

            if (icon != null && panel?.Children[1] is System.Windows.Controls.Image image)
            {
                image.Source = icon;
            }
        }

        public static BitmapSource ToImageSource(this Icon icon)
        {
            return Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromRect(ref RECT lprc, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        public static bool IsWindowPlacementVisible(double left, double top, double width = 256, double height = 256)
        {
            var rect = new RECT
            {
                Left = (int)left,
                Top = (int)top,
                Right = (int)(left + width),
                Bottom = (int)(top + height)
            };

            IntPtr monitor = MonitorFromRect(
                ref rect,
                MONITOR_DEFAULTTONEAREST);

            if (monitor == IntPtr.Zero)
                return false;

            var monitorInfo = new MONITORINFO
            {
                cbSize = Marshal.SizeOf<MONITORINFO>()
            };

            if (!GetMonitorInfo(monitor, ref monitorInfo))
                return false;

            var work = monitorInfo.rcWork;

            bool intersects =
                rect.Left < work.Right &&
                rect.Right > work.Left &&
                rect.Top < work.Bottom &&
                rect.Bottom > work.Top;

            return intersects;
        }
    }
}
