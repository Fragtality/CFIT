using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CFIT.AppTools
{
    public static class RegexExt
    {
        public static bool GroupsMatching(this Regex regex, string input, List<int> groupIndices, out List<string> groups)
        {
            groups = new List<string>();
            var matches = regex.Matches(input);
            if (matches?.Count == 0 || matches[0]?.Groups?.Count == 0 || groupIndices?.Count == 0 || string.IsNullOrWhiteSpace(input))
                return false;

            int i = 0;
            foreach (var grp in matches[0].Groups)
            {
                if (groupIndices.Contains(i))
                    groups.Add(grp.ToString());
                i++;
            }

            return groupIndices.Count == groups.Count;
        }
    }
}
