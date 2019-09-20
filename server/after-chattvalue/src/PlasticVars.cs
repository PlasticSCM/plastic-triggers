using System;
using System.Collections.Generic;

namespace JenkinsPlug
{
    internal class PlasticVars
    {
        internal string ObjectSpec;
        internal string AttrName;
        internal string AttrValue;
        internal string Repository;
        internal string Server;
        internal string User;
        internal string UserMachine;

        internal void LoadFromStdin()
        {
            PlasticVars vars = new PlasticVars();
            List<string> result = new List<string>();

            string line;
            while ((line = Console.ReadLine()) != null)
            {
                result.Add(line.Trim());
                Console.WriteLine("trigger line:[" + line + "]");
            }

            if (result.Count == 0)
                return;

            string pendingToParseLine = result[0];
            int indexOfFirstSeparator = pendingToParseLine.IndexOf(" ");
            if (indexOfFirstSeparator <= 0)
                return;

            ObjectSpec = pendingToParseLine.Substring(0, indexOfFirstSeparator).Trim();

            pendingToParseLine = result[0].Substring(indexOfFirstSeparator + 1);

            int indexOfAttrName = pendingToParseLine.IndexOf("attribute:");
            int indexOfValue = pendingToParseLine.IndexOf("value:");

            bool bIsValueLastArg = indexOfValue > indexOfAttrName;

            string attrNameParsed = string.Empty;
            string attrValueParsed = string.Empty;

            if (bIsValueLastArg)
            {
                attrValueParsed = pendingToParseLine.Substring(indexOfValue + "value:".Length);
                attrNameParsed = pendingToParseLine.Substring(indexOfAttrName + "attribute:".Length, indexOfValue  - "attribute:".Length);
            }
            else
            {
                attrNameParsed = pendingToParseLine.Substring(indexOfAttrName + "attribute:".Length);
                attrValueParsed = pendingToParseLine.Substring(indexOfValue + "value:".Length, indexOfAttrName - "value:".Length);
            }

            AttrValue = attrValueParsed.Replace("\"", string.Empty).Replace("'", string.Empty).Trim();
            AttrName = attrNameParsed.Replace("\"", string.Empty).Replace("'", string.Empty).Trim();            
        }

        internal bool Validate(out string errorConfigMsg)
        {
            errorConfigMsg = string.Empty;
            if (string.IsNullOrWhiteSpace(ObjectSpec))
                errorConfigMsg += "No object spec to switch to was provided in trigger vars. " + Environment.NewLine;
            if (string.IsNullOrWhiteSpace(AttrName))
                errorConfigMsg += "No attribute name specified in trigger vars. " + Environment.NewLine;
            if (string.IsNullOrWhiteSpace(AttrValue))
                errorConfigMsg += "No attribute value specified in trigger vars. " + Environment.NewLine;
            if (string.IsNullOrWhiteSpace(Server))
                errorConfigMsg += "No server name specified in trigger vars. " + Environment.NewLine;
            if (string.IsNullOrWhiteSpace(Repository))
                errorConfigMsg += "No repository name specified in trigger vars. " + Environment.NewLine;

            return string.IsNullOrWhiteSpace(errorConfigMsg);
        }

        internal void LoadFromEnvVars()
        {
            Repository = Environment.GetEnvironmentVariable("PLASTIC_REPOSITORY_NAME");
            Server = Environment.GetEnvironmentVariable("PLASTIC_SERVER");
            User = Environment.GetEnvironmentVariable("PLASTIC_USER");
            UserMachine = Environment.GetEnvironmentVariable("PLASTIC_CLIENTMACHINE");
        }
    }
}