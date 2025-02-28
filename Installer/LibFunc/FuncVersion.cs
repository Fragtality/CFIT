using System;
using System.Text.RegularExpressions;

namespace CFIT.Installer.LibFunc
{
    public static class FuncVersion
    {
        public static readonly Regex rxNumberMatch = new Regex(@"\D*(\d+)\D*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string[] CleanNumbers(string[] versions)
        {
            for (int i = 0; i < versions.Length; i++)
            {
                var match = rxNumberMatch.Match(versions[i]);
                if (match?.Groups?.Count == 2 && !string.IsNullOrWhiteSpace(match?.Groups[1]?.Value))
                    versions[i] = match.Groups[1].Value;
                else
                    return null;
            }

            return versions;
        }

        public enum VersionCompare
        {
            EQUAL = 1,
            LESS,
            LESS_EQUAL,
            GREATER,
            GREATER_EQUAL
        }

        public static bool CheckVersion(string leftVersion, VersionCompare comparison, string rightVersion, out bool compareable, bool majorEqual = false, int digits = 3)
        {
            compareable = false;

            if (string.IsNullOrWhiteSpace(leftVersion) || string.IsNullOrWhiteSpace(rightVersion))
                return false;

            string[] leftParts = leftVersion.Split('.');
            string[] rightParts = rightVersion.Split('.');
            if (leftParts.Length < digits || rightParts.Length < digits)
                return false;

            leftParts = CleanNumbers(leftParts);
            rightParts = CleanNumbers(rightParts);
            if (leftParts == null || rightParts == null)
                return false;

            leftVersion = string.Join(".", leftParts);
            rightVersion = string.Join(".", rightParts);
            if (!Version.TryParse(leftVersion, out Version left) || !Version.TryParse(rightVersion, out Version right))
                return false;

            compareable = true;

            if (majorEqual && left.Major != right.Major)
                return false;

            switch (comparison)
            {
                case VersionCompare.LESS:
                    return left < right;
                case VersionCompare.LESS_EQUAL:
                    return left <= right;
                case VersionCompare.GREATER:
                    return left > right;
                case VersionCompare.GREATER_EQUAL:
                    return left >= right;
                default:
                    return left == right;
            }
        }
    }
}
