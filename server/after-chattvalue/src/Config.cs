using System;
using System.Collections.Generic;
using System.IO;

namespace JenkinsTrigger
{
    public class Config
    {
        public string Url;
        public string User;
        public string Password;
        public string JenkinsJob;
        public string AttributeNameToWatch;
        public string AttributeValueToWatch;
        public string RawReposToWatch;
        public string RawObjectSpecPrefixesToSkip;

        internal static Config Parse(string confFilePath)
        {
            List<string> result = new List<string>();

            string line = string.Empty;
            using (StreamReader sr = new StreamReader(confFilePath))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (line.StartsWith("#"))
                        continue;

                    if (line.StartsWith("["))
                        continue;

                    result.Add(line);
                }
            }

            if (result.Count == 0)
                return null;

            Config config = new Config();

            foreach (string arg in result)
            {
                if (string.IsNullOrWhiteSpace(arg))
                    continue;

                if (arg.StartsWith("url", StringComparison.InvariantCultureIgnoreCase))
                {
                    config.Url = ParseArgValue(arg);
                    continue;
                }
                if (arg.StartsWith("user", StringComparison.InvariantCultureIgnoreCase))
                {
                    config.User = ParseArgValue(arg);
                    continue;
                }
                if (arg.StartsWith("password", StringComparison.InvariantCultureIgnoreCase))
                {
                    config.Password = ParseArgValue(arg);
                    continue;
                }
                if (arg.StartsWith("job", StringComparison.InvariantCultureIgnoreCase))
                {
                    config.JenkinsJob = ParseArgValue(arg);
                    continue;
                }
                if (arg.StartsWith("attrname", StringComparison.InvariantCultureIgnoreCase))
                {
                    config.AttributeNameToWatch = ParseArgValue(arg);
                    continue;
                }

                if (arg.StartsWith("attrvalue", StringComparison.InvariantCultureIgnoreCase))
                {
                    config.AttributeValueToWatch = ParseArgValue(arg);
                    continue;
                }
                if (arg.StartsWith("skipprefixes", StringComparison.InvariantCultureIgnoreCase))
                {
                    config.RawObjectSpecPrefixesToSkip = ParseArgValue(arg);
                    continue;
                }
                if (arg.StartsWith("repositories", StringComparison.InvariantCultureIgnoreCase))
                {
                    config.RawReposToWatch = ParseArgValue(arg);
                    continue;
                }
            }

            return config;
        }

        internal bool Validate(out string errorConfigMsg)
        {
            errorConfigMsg = string.Empty;
            if (string.IsNullOrWhiteSpace(Url))
                errorConfigMsg += "No jenkins url specified in config file. " + Environment.NewLine;
            if (string.IsNullOrWhiteSpace(User))
                errorConfigMsg += "No jenkins user specified in config file. " + Environment.NewLine;
            if (string.IsNullOrWhiteSpace(Password))
                errorConfigMsg += "No jenkins password specified in config file. " + Environment.NewLine;
            if (string.IsNullOrWhiteSpace(JenkinsJob))
                errorConfigMsg += "No jenkins job specified in config file. " + Environment.NewLine;
            if (string.IsNullOrWhiteSpace(AttributeNameToWatch))
                errorConfigMsg += "No plastic status attribute name to watch specified in config file. " + Environment.NewLine;
            if (string.IsNullOrWhiteSpace(AttributeValueToWatch))
                errorConfigMsg += "No plastic status attribute value to watch specified in config file. " + Environment.NewLine;

            return string.IsNullOrWhiteSpace(errorConfigMsg);
        }

        static string ParseArgValue(string arg)
        {
            int separatorIndex = arg.IndexOf("=");
            if (separatorIndex <= 0)
            {
                Console.WriteLine("Unable to parse arg: " + arg);
                return string.Empty;
            }

            return arg.Substring(separatorIndex + 1).Trim();
        }
    }
}
