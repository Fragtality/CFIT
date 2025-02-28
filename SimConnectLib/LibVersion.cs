using System;
using System.Reflection;

namespace CFIT.SimConnectLib
{
    public static class LibVersion
    {
        public static Version Version { get { return Assembly.GetExecutingAssembly()?.GetName()?.Version ?? new Version(); } }
    }
}
