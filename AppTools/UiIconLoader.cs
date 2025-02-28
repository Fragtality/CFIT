using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CFIT.AppTools
{
    public enum IconLoadSource
    {
        Embedded = 1,
        Resource = 2,
    }

    public class UiIconLoader
    {
        public virtual IconLoadSource Source { get; }
        public virtual Assembly Assembly { get; }
        public virtual string Prefix { get; }
        public virtual string Extension { get; }

        public UiIconLoader(Assembly assembly, IconLoadSource source, string prefix = "", string extension = ".png")
        {
            Source = source;
            Assembly = assembly;
            Prefix = prefix;
            Extension = extension;
        }

        public UiIconLoader(IconLoadSource source, string prefix = "", string extension = ".png") : this(Assembly.GetExecutingAssembly(), source, prefix, extension)
        {

        }

        public virtual void SetImage(Image image, string icon)
        {
            var bitmap = LoadIcon(icon);
            if (icon != null)
                image.Source = bitmap;
        }

        public virtual BitmapImage LoadIcon(string icon)
        {
            if (Source == IconLoadSource.Embedded)
                return Gui.GetBitmapImageFromResourceManifest(icon, Prefix, Assembly, Extension);
            else if (Source == IconLoadSource.Resource)
                return Gui.GetBitmapImageFromPackUri(icon, Prefix, Extension);
            else
                return null;
        }
    }
}
