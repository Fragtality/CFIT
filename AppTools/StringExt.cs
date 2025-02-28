using System;
using System.Linq;
using System.Text;

namespace CFIT.AppTools
{
    public static class StringExt
    {
        public static string Compact(this string value, int num = 24, string prefix = "...")
        {
            if (value?.Length <= num)
                return value;
            else
                return $"{prefix}{value.Substring(value.Length - num)}";
        }

        public static bool HasArgument(this string[] args, string arg)
        {
            return args?.Any(a => a?.ToLowerInvariant() == arg?.ToLowerInvariant()) == true;
        }

        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
