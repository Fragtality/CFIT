using CFIT.AppTools;
using System.Reflection;

namespace CFIT.Installer.UI
{
    public class IconLoader : UiIconLoader
    {
        //public IconLoader(IconLoadSource source) : base(source, source == IconLoadSource.Embedded ? "CFIT.Installer.icons." : "/icons/", ".png")
        //{

        //}

        public static IconLoader Instance { get; } = new IconLoader(IconLoadSource.Embedded);

        public IconLoader(IconLoadSource source) : base(Assembly.GetExecutingAssembly(), source, source == IconLoadSource.Embedded ? "CFIT.Installer.icons." : "/icons/", ".png")
        {

        }
    }
}
