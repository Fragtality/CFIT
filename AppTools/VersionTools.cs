using System;
using System.Reflection;

namespace CFIT.AppTools
{
    public static class VersionTools
    {
        public static Version GetExecutingAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly()?.GetName()?.Version ?? new Version();
        }

        public static string GetExecutingAssemblyVersion(int fields)
        {
            return Assembly.GetExecutingAssembly()?.GetName()?.Version.ToString(fields) ?? "";
        }

        public static Version GetEntryAssemblyVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Version ?? new Version();
        }

        public static string GetEntryAssemblyVersion(int fields)
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Version.ToString(fields) ?? "";
        }

        public static string GetExecutingAssemblyTimestamp(string format = ProductVersion.TimestampFormat)
        {
            return Assembly.GetExecutingAssembly().GetLinkerTime().ToString(format);
        }

        public static string GetEntryAssemblyTimestamp(string format = ProductVersion.TimestampFormat)
        {
            return Assembly.GetEntryAssembly().GetLinkerTime().ToString(format);
        }
    }
}