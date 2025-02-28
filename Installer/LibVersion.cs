using System;
using System.Reflection;

namespace CFIT.Installer
{
    public static class LibVersion
    {
        public static Version Version { get { return Assembly.GetExecutingAssembly()?.GetName()?.Version ?? new Version(); } }
    }
}
