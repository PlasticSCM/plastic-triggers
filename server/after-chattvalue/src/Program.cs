using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace JenkinsTrigger
{
    class Program
    {
        static int Main(string[] args)
        {
            System.Threading.Thread.Sleep(10000);
            try
            {
                string confFilePath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "jenkinstrigger.conf"));

                if (!File.Exists(confFilePath))
                {
                    Console.WriteLine("Cannot run the trigger if the config file does not exist: " + confFilePath);
                    return 1;
                }

                Config config = Config.Parse(confFilePath);
                if (config == null)
                {
                    Console.WriteLine("Unable to parse the config file: " + confFilePath);
                    return 1;
                }

                string errorConfigMsg = string.Empty;

                PlasticVars plasticVars = new PlasticVars();
                plasticVars.LoadFromStdin();
                plasticVars.LoadFromEnvVars();

                if (!plasticVars.Validate(out errorConfigMsg))
                {
                    Console.WriteLine("Invalid trigger environment args. Won't trigger build. Message: " + errorConfigMsg);
                    return 0;
                }

                string plasticObjectSpec = BuildPlasticObjectSpec(plasticVars);

                if (!TriggerRules.HasToLaunchBuild(config, plasticVars, plasticObjectSpec))
                {
                    Console.WriteLine("No need to launch a jenkins build with specified settings.");
                    return 0;
                }

                if (!config.Validate(out errorConfigMsg))
                {
                    Console.WriteLine(
                        "Trigger config not valid. Unable to launch jenkins build for [" +
                        plasticObjectSpec +
                        "]. Review the trigger configuration file " +
                        "[" + 
                        confFilePath + 
                        "]. Message:" + 
                        errorConfigMsg);

                    return 1;
                }

                Dictionary<string, string> jenkinsEnvVars = BuildJenkinsEnvVars(plasticVars, plasticObjectSpec);

                string authToken = HttpClientBuilder.GetAuthToken(config.User, config.Password);

                using (HttpClient httpClient = HttpClientBuilder.Build(config.Url, authToken))
                {
                    if (!JenkinsBuild.CheckConnection(httpClient))
                    {
                        Console.WriteLine("ERROR: Unable to check conn with jenkins: [" + config.Url + "] with specified crendentials.");
                        return 1;
                    }

                    string id = JenkinsBuild.QueueBuildAsync(
                        config.JenkinsJob,
                        plasticObjectSpec, 
                        "build from jenkins trigger",
                        jenkinsEnvVars, 
                        httpClient).Result;

                    if (string.IsNullOrEmpty(id))
                    {
                        Console.WriteLine("The trigger was unable to send the build request to jenkins");
                        return 1;
                    }

                    return 0;
                }           
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                Console.WriteLine("StackTrace: " + e.StackTrace);
                return 1;
            }
        }

        static Dictionary<string, string> BuildJenkinsEnvVars(PlasticVars plasticVars, string plasticObjectSpec)
        {
            Dictionary<string, string> jenkinsVars = new Dictionary<string, string>();
            jenkinsVars["REPSPEC"] = plasticVars.Repository + "@" + plasticVars.Server;
            jenkinsVars["PLASTIC_USER"] = plasticVars.User;
            jenkinsVars["PLASTIC_USERMACHINE"] = plasticVars.UserMachine;

            return jenkinsVars;            
        }

        static string BuildPlasticObjectSpec(PlasticVars plasticVars)
        {
            if (plasticVars.ObjectSpec.IndexOf("@") > 0)
                return plasticVars.ObjectSpec;

            return 
                plasticVars.ObjectSpec + 
                "@" +
                plasticVars.Repository + 
                "@" + 
                plasticVars.Server;
        }

        

        static class HttpClientBuilder
        {
            internal static string GetAuthToken(string user, string password)
            {
                return Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(user + ":" + password));
            }

            internal static HttpClient Build(string host, string authHeader)
            {
                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(host);

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

                httpClient.DefaultRequestHeaders.ConnectionClose = false;
                return httpClient;
            }
        }
    }
}
