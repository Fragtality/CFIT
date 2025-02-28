using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace CFIT.AppTools
{
    public static class AssemblyTools
    {
        public static DateTime GetLinkerTime(this Assembly assembly)
        {
            try
            {
                const string BuildVersionMetadataPrefix = "+build";
                const string dateFormat = "yyyy.MM.dd.HHmm";

                var attribute = assembly
                  .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

                if (attribute?.InformationalVersion != null)
                {
                    var value = attribute.InformationalVersion;
                    var index = value.IndexOf(BuildVersionMetadataPrefix);
                    if (index > 0)
                    {
                        value = value.Substring(index + BuildVersionMetadataPrefix.Length);

                        return DateTime.ParseExact(
                            value,
                          dateFormat,
                          CultureInfo.InvariantCulture);
                    }
                }
            }
            catch { }

            return default;
        }

        public static Stream GetStreamFromAssembly(string name, bool executing = false)
        {
            try
            {
                if (!executing)
                    return Assembly.GetEntryAssembly().GetManifestResourceStream(name);
                else
                    return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            }
            catch { }
            return null;
        }
    }
}
