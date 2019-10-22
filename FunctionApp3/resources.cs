using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace FunctionApp3
{
    public static class Function1
    {
        [FunctionName("resources")]
        public static async Task RunAsync([TimerTrigger("0 0 4 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        // environment variables
            string tenantId = Environment.GetEnvironmentVariable("tenantId");
            string clientId = Environment.GetEnvironmentVariable("appIdSV");
            string clientKey = Environment.GetEnvironmentVariable("appKeySV");
            string subId = Environment.GetEnvironmentVariable("subIdSV");
            string storageName = Environment.GetEnvironmentVariable("storageName");
            string storageKey = Environment.GetEnvironmentVariable("storageKey");
            string containerName = "resources-sv";

        // reformat DateTime, as / and : not usable in filenames
            // include 'dateFull.Substring(17, 2)' to specify seconds (use 11, 2 for hours and 14, 2 for mins) when run. So that name is unique when running frequently for testing
            // formated as dd-mm-yyyy
            string dateFull = DateTime.Now.ToString();
            string date = dateFull.Substring(3, 2) + "-" + dateFull.Substring(0, 2) + "-" + dateFull.Substring(6, 4);


        // define Azure Stroage Account
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=" + storageName + ";AccountKey=" + storageKey + ";EndpointSuffix=core.windows.net";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

        // aquire bearer token, variable given when creating service principal. Only needs to run once (token is valid for 1 year by default) but currently runs everytime
            string authContextURL = "https://login.windows.net/" + tenantId;
            AuthenticationContext authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId, clientKey);
            var result = await authenticationContext
            .AcquireTokenAsync("https://management.azure.com/", credential);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the token");
            }
            string token = result.AccessToken;


            // create and upload All Resources file
            string responseBody = await Helper.GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resources?api-version=2017-05-10", token);

            string body = Helper.FormatResponse(responseBody);

            await Helper.OutputCloudAsync(blobClient, containerName, date + "-AllResources", body);


        // create and upload Resources by resource group files
            string responseBodyRG = await Helper.GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resourceGroups?api-version=2014-04-01", token);

            // create a List containing name of each resource group
            List<string> resourceGroupsList = new List<string>();
            int start = 0;
            int end = 0;
            for (int i = 0; i < responseBodyRG.Length - 13; i++)
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

                    Console.WriteLine("Error: {0}", outOfRange.Message);
                }
            }

            // output resources with a folder structure, Resource_Groups > resource group name > dated resources file
            foreach (string resourceGroup in resourceGroupsList)
            {
                string responseBodyRGResources = await Helper.GetInfoAsync("https://management.azure.com/subscriptions/" + Environment.GetEnvironmentVariable("subIdSV") + "/resourceGroups/" + resourceGroup + "/resources?api-version=2017-05-10", token);
                
                string bodyRG = Helper.FormatResponse(responseBodyRGResources);

                await Helper.OutputCloudAsync(blobClient, containerName, "ResourceGroups/" + resourceGroup + "/" + date + "-" + resourceGroup, bodyRG);
            }
        }
    }
}
