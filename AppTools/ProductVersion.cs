using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFIT.AppTools
{
    public class ProductVersion
    {
        public string Version { get; set; }
        [JsonIgnore]
        public Version VersionParsed { get { return new Version(Version); } }
        public string Timestamp { get; set; }

        [JsonIgnore]
        public const string TimestampFormat = "yyyy.MM.dd.HHmm";

        public ProductVersion()
        {
            Version = "1.0.0";
            Timestamp = "1970.01.01.0001";
        }

        public static ProductVersion GetProductVersionFromStream(string resPath)
        {
            ProductVersion version = null;
            try
            {
                using (var stream = AssemblyTools.GetStreamFromAssembly(resPath))
                {
                    version = JsonSerializer.Deserialize<ProductVersion>(stream);
                }
            }
            catch { }

            return version;
        }

        public static ProductVersion GetProductVersionFromFile(string file)
        {
            ProductVersion version = null;
            try
            {
                version = JsonSerializer.Deserialize<ProductVersion>(File.ReadAllText(file));
            }
            catch { }

            return version;
        }
    }
}
