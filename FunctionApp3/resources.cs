using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.IO;
using System.Text;
using System.Net.Http;

namespace FunctionApp3
{
    public static class Function1
    {
        [FunctionName("resources")]
        public static async Task RunAsync([TimerTrigger("0 0 4 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // variables
            string tenantId = Environment.GetEnvironmentVariable("tenantIdCANCOM");
            string clientId = Environment.GetEnvironmentVariable("appIdSV");
            string clientKey = Environment.GetEnvironmentVariable("appKeySV");
            string subId = Environment.GetEnvironmentVariable("subIdSV");
            string storageName = Environment.GetEnvironmentVariable("storageName");
            string storageKey = Environment.GetEnvironmentVariable("storageKey");
            string containerName = "resources-sv";

            // reformat DateTime.Now, as / and : not usable in filenames
            string dateFormatted = FormatDateTime();

            // aquire bearer token for the service principle
            string token = await AquireBearerTokenAsync(tenantId, clientId, clientKey);

            // define Azure Stroage Account
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=" + storageName + ";AccountKey=" + storageKey + ";EndpointSuffix=core.windows.net";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // create and upload All Resources file
            string responseBody = await GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resources?api-version=2017-05-10", token);
            string body = FormatResponse(responseBody);
            await OutputCloudAsync(blobClient, containerName, "AllResources" + dateFormatted + ".txt", body);

            // create and upload Resources files by resource group
            string responseBodyRG = await GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resourceGroups?api-version=2014-04-01", token);
            List<string> resourceGroupsList = ListResourceGroups(responseBodyRG, log);

            // output resources with a folder structure, ResourceGroups > resource group name > dated resources file
            foreach (string resourceGroup in resourceGroupsList)
            {
                string responseBodyRGResources = await GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resourceGroups/" + resourceGroup + "/resources?api-version=2017-05-10", token);
                string bodyRG = FormatResponse(responseBodyRGResources);
                await OutputCloudAsync(blobClient, containerName, "ResourceGroups/" + resourceGroup + "/" + resourceGroup + "Resources" + dateFormatted + ".txt", bodyRG);
            }
        }

        // include 'dateFull.Substring(17, 2)' to specify seconds (use 11, 2 for hours and 14, 2 for mins) so that fileName is unique when running frequently for testing,
        // above is true only after the 10th of Oct, Nov, Dec; indices vary as single digit days/months are formatted as 'n' not '0n' in Azure Function Apps
        // formated as dd-mm-yyyy in Azure Function App, the DateTime struct returns differently formatted result in Visual Studio, Azure CLI and Azure Function Apps :@
        public static string FormatDateTime()
        {
            string dateFull = DateTime.Now.ToString();
            string dateFormatted = "";
            if (dateFull.Substring(1, 1) == "/" && dateFull.Substring(3, 1) == "/")
            {
                dateFormatted = dateFull.Substring(2, 1) + "-" + dateFull.Substring(0, 1) + "-" + dateFull.Substring(4, 4);
            }
            if (dateFull.Substring(1, 1) == "/" && dateFull.Substring(4, 1) == "/")
            {
                dateFormatted = dateFull.Substring(2, 2) + "-" + dateFull.Substring(0, 1) + "-" + dateFull.Substring(5, 4);
            }
            if (dateFull.Substring(2, 1) == "/" && dateFull.Substring(4, 1) == "/")
            {
                dateFormatted = dateFull.Substring(3, 1) + "-" + dateFull.Substring(0, 2) + "-" + dateFull.Substring(5, 4);
            }
            if (dateFull.Substring(2, 1) == "/" && dateFull.Substring(5, 1) == "/")
            {
                dateFormatted = dateFull.Substring(3, 2) + "-" + dateFull.Substring(0, 2) + "-" + dateFull.Substring(6, 4);
            }
            return dateFormatted;
        }

        // aquire bearer token, variables given when creating service principal. Only needs to run once (token is valid for 1 year by default) but currently runs everytime function is triggered
        public static async Task<string> AquireBearerTokenAsync(string tenantId, string clientId, string clientKey)
        { 
            string authContextURL = "https://login.windows.net/" + tenantId;
            AuthenticationContext authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId, clientKey);
            var result = await authenticationContext.AcquireTokenAsync("https://management.azure.com/", credential);
                if (result == null)
                {
                    throw new InvalidOperationException("Failed to obtain the token");
                }
            return result.AccessToken;
        }

        // create a List containing name of each resource group in the target subscription
        public static List<string> ListResourceGroups(string responseBodyRG, ILogger log)
        {
            List<string> resourceGroupsList = new List<string>();
            int start = 0;
            int end = 0;
            for (int i = 0; i < responseBodyRG.Length - 12; i++)
            {
                try
                {
                    if (responseBodyRG.Substring(i, 6) == "name\":")
                    {
                        start = i + 7;
                    }
                    if (responseBodyRG.Substring(i, 10) == "location\":")
                    {
                        end = i - 3;
                        resourceGroupsList.Add(responseBodyRG.Substring(start, end - start));
                    }
                }
                catch (ArgumentOutOfRangeException outOfRange)
                {
                    log.LogInformation("Error: {0} RG", outOfRange.Message);
                }
            }
            return resourceGroupsList;
        }


        // send a get request for resources, with bearer token in header
        public static async Task<string> GetInfoAsync(string URL, string token)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpResponseMessage response = await httpClient.GetAsync(URL);
            return await response.Content.ReadAsStringAsync();
        }


        // request response (json parsed to string) is formated one line per resource
        public static string FormatResponse(string responseBody)
        {
            string body = "";
            int start = 0;
            for (int i = 0; i < responseBody.Length - 1; i++)
            {
                try
                {

                    if (responseBody.Substring(i, 1) == "[")
                    {
                        body += "[\n";
                        start = i + 1;
                    }
                    if (responseBody.Substring(i, 2) == ",{")
                    {
                        body += responseBody.Substring(start, i - start) + "\n";
                        start = i + 1;
                    }
                    if (responseBody.Substring(i, 1) == "]")
                    {
                        body += responseBody.Substring(start, i - start) + "\n";
                        body += "]";
                    }
                }
                catch (ArgumentOutOfRangeException outOfRange)
                {

                    Console.WriteLine("Error: {0} format", outOfRange.Message);
                }
            }
            return body;
        }

        public static void OutputLocal(string body, string dateFormatted)
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, $"Resources{dateFormatted}.txt")))
            {
                outputFile.Write(body);
            }
        }

        // Create container and upload file, include / in fileName to create folders
        public static async Task OutputCloudAsync(CloudBlobClient blobClient, string containerName, string fileName, string body)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
            {
                await blockBlob.UploadFromStreamAsync(stream);
            }
        }
    }
}
