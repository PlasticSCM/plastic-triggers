using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace JenkinsTrigger
{
    internal static class JenkinsBuild
    {
        internal class Crumb
        {
            internal string FieldName;
            internal string Value;
        }

        internal static bool CheckConnection(HttpClient httpClient)
        {
            HttpResponseMessage response = null;
            try
            {
                response = GetWithRetriesAsync(httpClient, GET_QUEUE_URI).Result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.Log(ex);
                return false;
            }

            return response.IsSuccessStatusCode;
        }

        internal static async Task<string> QueueBuildAsync(
            string projectName,
            string plasticUpdateToSpec,
            string buildComment,
            Dictionary<string, string> botRequestProperties,
            HttpClient httpClient)
        {
            XmlDocument projectDescriptor = null;
            bool bXmlVersionChanged = false;

            string projectDescriptorContents = await GetProjectDescriptorAsync(projectName, httpClient);

            projectDescriptorContents = ProjectXmlVersionFix.EnsureV1_0(
                projectDescriptorContents, out bXmlVersionChanged);

            projectDescriptor = ProjectDescriptor.Parse(projectDescriptorContents);

            List<BuildProperty> requestProperties = QueueBuildRequestProps.Create(plasticUpdateToSpec, botRequestProperties);
            string projectAuthToken = string.Empty;

            projectAuthToken = ProjectDescriptor.GetAuthToken(projectDescriptor);

            List<string> pendingParametersToConfigure =
                ProjectDescriptor.GetMissingParameters(projectDescriptor, requestProperties);

            if (pendingParametersToConfigure.Count > 0)
                await ModifyJenkinsProjectAsync(
                    httpClient,
                    projectName,
                    projectDescriptor,
                    pendingParametersToConfigure,
                    bXmlVersionChanged);

            if (!string.IsNullOrEmpty(projectAuthToken))
                requestProperties.Add(new BuildProperty("token", projectAuthToken));

            if (!string.IsNullOrEmpty(buildComment))
                requestProperties.Add(new BuildProperty("cause", buildComment));

            string endPoint = Uri.EscapeUriString(
                string.Format(
                    QUEUE_BUILD_URI_FORMAT,
                    projectName,
                    BuildPropertiesUri(requestProperties)));

            HttpResponseMessage response = await PostWithRetriesAsync(httpClient, endPoint, null);

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            if (response.Headers.Location != null)
                return GetBuildNumberFromLocationPathHeader(
                    response.Headers.Location.AbsolutePath);

            return string.Empty;
        }

        internal static async Task<string> GetProjectDescriptorAsync(
            string projectName,
            HttpClient httpClient)
        {
            string endPoint = string.Format(GET_JOB_CONFIG_URI, projectName);
            return await GetStringResponseAsync(endPoint, httpClient);
        }

        static async Task<string> GetStringResponseAsync(string endPoint, HttpClient httpClient)
        {
            HttpResponseMessage response = await GetWithRetriesAsync(httpClient, endPoint);

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            return await response.Content.ReadAsStringAsync();
        }

        static async Task ModifyJenkinsProjectAsync(
            HttpClient httpClient,
            string projectName,
            XmlDocument projectDescriptor,
            List<string> pendingParametersToConfigure,
            bool bXmlVersionChanged)
        {
            string modifiedXmlProject = Path.GetTempFileName();

            try
            {
                ProjectDescriptor.AddMissingParameters(
                    projectDescriptor,
                    pendingParametersToConfigure,
                    modifiedXmlProject);

                string payLoadStr = File.ReadAllText(modifiedXmlProject);
                if (bXmlVersionChanged)
                    payLoadStr = ProjectXmlVersionFix.RestoreToV1_1(payLoadStr);

                var payLoad = new StringContent(
                    payLoadStr,
                    System.Text.Encoding.UTF8,
                    "application/xml");

                string endPoint = string.Format(GET_JOB_CONFIG_URI, projectName);
                HttpResponseMessage response = await PostWithRetriesAsync(httpClient, endPoint, payLoad);

                if (response.IsSuccessStatusCode)
                    return;

                throw new InvalidOperationException(string.Format(
                    "Unable to update config.xml file for project [{0}] " +
                    "in order to setup required build parameters: {1}",
                    projectName, response.ReasonPhrase));
            }
            finally
            {
                if (File.Exists(modifiedXmlProject))
                    File.Delete(modifiedXmlProject);
            }
        }

        static string BuildPropertiesUri(List<BuildProperty> payloadProps)
        {
            string result = string.Empty;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < payloadProps.Count; i++)
            {
                sb.AppendFormat("{0}{1}={2}",
                    i == 0 ? string.Empty : "&",
                    payloadProps[i].Name,
                    payloadProps[i].Value);
            }

            return sb.ToString();
        }

        static string GetBuildNumberFromLocationPathHeader(string absolutePath)
        {
            Console.WriteLine(string.Format(
                "Get Build Number of queued job - " +
                "Absolute path returned from Jenkins Server: {0}", absolutePath));

            //absolutepath is sth like "/.../queue/item/675/"
            string[] parts = absolutePath.Trim().Split(
                new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts == null || parts.Length == 0)
                return string.Empty;

            return parts[parts.Length - 1];
        }

        static async Task<HttpResponseMessage> GetWithRetriesAsync(HttpClient httpClient, string buildIdEndpoint)
        {
            int retries = 0;
            Console.WriteLine(string.Format("http Get: [{0}]", buildIdEndpoint));
            while (true)
            {
                try
                {
                    UpdateCrumb(httpClient);

                    return await httpClient.GetAsync(buildIdEndpoint);
                }
                catch (WebException e)
                {
                    Console.WriteLine(string.Format(
                        "Error in GetAsync on endpoint [{0}] after {1} retries (max retries: {2}). Error:{3}",
                        buildIdEndpoint,
                        retries,
                        MAX_ASYNC_RETRIES,
                        ExceptionLogger.GetMessageToLog(e)));

                    ++retries;

                    if (retries > MAX_ASYNC_RETRIES)
                        throw;

                    Task.Delay(retries * WAIT_MILLIS_ASYNC).Wait();
                    continue;
                }
            }
        }

        static async Task<HttpResponseMessage> PostWithRetriesAsync(HttpClient httpClient, string endpoint, HttpContent content)
        {
            int retries = 0;
            Console.WriteLine(string.Format("http Post: [{0}]", endpoint));
            while (true)
            {
                try
                {
                    UpdateCrumb(httpClient);

                    return await httpClient.PostAsync(endpoint, content);
                }
                catch (WebException e)
                {
                    Console.WriteLine(string.Format(
                        "Error in PostAsync on endpoint [{0}] after {1} retries (max retries: {2}). Error:{3}",
                        endpoint,
                        retries,
                        MAX_ASYNC_RETRIES,
                        ExceptionLogger.GetMessageToLog(e)));

                    ++retries;

                    if (retries > MAX_ASYNC_RETRIES)
                        throw;

                    Task.Delay(retries * WAIT_MILLIS_ASYNC).Wait();
                    continue;
                }
            }
        }

        static void UpdateCrumb(HttpClient httpClient)
        {
            JenkinsBuild.Crumb crumb = JenkinsBuild.GetCrumb(httpClient).Result;

            if (crumb == null)
            {
                Console.WriteLine("Unable to update crumb. Maybe CSRF settings are not set in your jenkins server.");
                return;
            }

            if (httpClient.DefaultRequestHeaders.Contains(crumb.FieldName))
            {
                httpClient.DefaultRequestHeaders.Remove(crumb.FieldName);
            }

            httpClient.DefaultRequestHeaders.Add(crumb.FieldName, crumb.Value);
        }

        static async Task<Crumb> GetCrumb(HttpClient httpClient)
        {
            string endPoint = string.Format(GET_CRUMB_URI);

            HttpResponseMessage response = await httpClient.GetAsync(endPoint);

            if (!response.IsSuccessStatusCode)
                return null;

            string responseContents = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseContents))
                return null;

            Crumb crumb = new Crumb();

            crumb.FieldName = XmlNodeLoader.LoadValue(
                responseContents, "/defaultCrumbIssuer/crumbRequestField");
            crumb.Value = XmlNodeLoader.LoadValue(
                responseContents, "/defaultCrumbIssuer/crumb");

            return crumb;
        }

        const string GET_CRUMB_URI = "crumbIssuer/api/xml";
        const string GET_JOB_CONFIG_URI = "job/{0}/config.xml";

        const string QUEUE_BUILD_URI_FORMAT = "job/{0}/buildWithParameters?{1}";

        const string GET_QUEUE_URI = "queue/api/xml";

        const int MAX_ASYNC_RETRIES = 5;
        const int WAIT_MILLIS_ASYNC = 500;
    }
}