using System.Text.RegularExpressions;

namespace EmailBugTracker.Logic
{
    public static class EmailAnonymization
    {
        /// <summary>
        /// Given an email this will reformat it as:
        /// *@*.tld
        /// Prefered format is 2 chars for domain and alias each, unless either is less than 3 chars
        /// </summary>
        /// <param name="from"></param>
        public static string PseudoAnonymize(string from)
        {
            if (string.IsNullOrEmpty(from) ||
                !from.Contains("@") ||
                !from.Contains("."))
                return "invalid email address";

            // this ignores the fact that .co.uk and simlar exist
            var regex = new Regex(@"(.*)@(.*)\.(.*)");
            var match = regex.Match(from);
            if (!match.Success)
                return "invalid email address";

            var alias = match.Groups[1].Value;
            var domain = match.Groups[2].Value;
            var tld = match.Groups[3].Value;

            string KeepFirst2Letters(string input)
            {
                if (input.Length <= 2)
                    return "**";
                return input.Substring(0, 2) + "****";
            }

            return $"{KeepFirst2Letters(alias)}@{KeepFirst2Letters(domain)}.{tld}";
        }
    }
}
