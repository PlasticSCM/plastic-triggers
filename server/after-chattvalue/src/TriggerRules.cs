using System;
using System.Text.RegularExpressions;

namespace JenkinsTrigger
{
    internal static class TriggerRules
    {
        internal static bool HasToLaunchBuild(Config config, PlasticVars plasticVars, string plasticObjectSpec)
        {
            return
                IsRepositoryMatch(config.RawReposToWatch, plasticVars.Repository) &&
                !IsSpecMarkedToSkip(config.RawObjectSpecPrefixesToSkip, plasticObjectSpec) &&
                IsAttributeMatch(config, plasticVars);
        }

        static bool IsRepositoryMatch(string rawReposToWatch, string repository)
        {
            if (string.IsNullOrWhiteSpace(rawReposToWatch))
                return true;

            string[] reposToWatch = StringSplitter.SplitList(rawReposToWatch);

            if (reposToWatch == null || reposToWatch.Length == 0)
                return false;

            foreach (string repoConfiguredToWatch in reposToWatch)
            {
                if (string.IsNullOrWhiteSpace(repoConfiguredToWatch))
                    continue;

                if (!Regex.IsMatch(repository.Trim(), repoConfiguredToWatch.Trim()))
                    continue;

                return true;
            }

            return false;
        }

        static bool IsSpecMarkedToSkip(string rawObjectSpecPrefixesToSkip, string plasticObjectSpec)
        {
            if (string.IsNullOrWhiteSpace(rawObjectSpecPrefixesToSkip))
                return false;

            string[] filters = StringSplitter.SplitList(rawObjectSpecPrefixesToSkip);

            if (filters == null || filters.Length == 0)
                return false;

            foreach (string filter in filters)
            {
                if (string.IsNullOrWhiteSpace(filter))
                    continue;

                if (!Regex.IsMatch(plasticObjectSpec.Trim(), filter.Trim()))
                    continue;

                return true;
            }

            return false;
        }

        static bool IsAttributeMatch(Config config, PlasticVars plasticVars)
        {
            if (string.IsNullOrEmpty(config.AttributeNameToWatch) ||
                string.IsNullOrEmpty(config.AttributeValueToWatch))
            {
                //will be reported later in config file params check, but at this point we avoid a nullref.
                return true; 
            }

            return
                config.AttributeNameToWatch.Equals(plasticVars.AttrName, StringComparison.InvariantCultureIgnoreCase) &&
                config.AttributeValueToWatch.Equals(plasticVars.AttrValue, StringComparison.InvariantCultureIgnoreCase);
        }

        class StringSplitter
        {
            internal static string[] SplitList(string rawStringList)
            {
                return rawStringList.Split(
                    new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            }
        }
    }
}
