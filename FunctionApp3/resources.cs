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

        // variables
            string tenantId = Environment.GetEnvironmentVariable("tenantIdCANCOM");
            string clientId = Environment.GetEnvironmentVariable("appIdSV");
            string clientKey = Environment.GetEnvironmentVariable("appKeySV");
            string subId = Environment.GetEnvironmentVariable("subIdSV");
            string storageName = Environment.GetEnvironmentVariable("storageName");
            string storageKey = Environment.GetEnvironmentVariable("storageKey");
            string containerName = "resources-sv";

        // reformat DateTime, as / and : not usable in filenames
            // include 'dateFull.Substring(17, 2)' to specify seconds (use 11, 2 for hours and 14, 2 for mins) so that fileName is unique when running frequently for testing,
            // above is true only after the 10th of Oct, Nov, Dec; indices vary as single digit days/months are formatted as 'n' not '0x' in Azure Function Apps
            // formated as dd-mm-yyyy in Azure Function App, the DateTime struct returns differently formatted result in Visual Studio, Azure CLI and Azure Function Apps :@
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

        // define Azure Stroage Account
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=" + storageName + ";AccountKey=" + storageKey + ";EndpointSuffix=core.windows.net";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

        // aquire bearer token, variable given when creating service principal. Only needs to run once (token is valid for 1 year by default) but currently runs everytime function is triggered
            string authContextURL = "https://login.windows.net/" + tenantId;
            AuthenticationContext authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId, clientKey);
            var result = await authenticationContext.AcquireTokenAsync("https://management.azure.com/", credential);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the token");
            }
            string token = result.AccessToken;

        // create and upload All Resources file
            string responseBody = await Helper.GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resources?api-version=2017-05-10", token);

            string body = Helper.FormatResponse(responseBody);

            await Helper.OutputCloudAsync(blobClient, containerName, dateFormatted + "-AllResources", body);


        // create and upload Resources files by resource group
            string responseBodyRG = await Helper.GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resourceGroups?api-version=2014-04-01", token);

            // create a List containing name of each resource group
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

            // output resources with a folder structure, ResourceGroups > resource group name > dated resources file
            foreach (string resourceGroup in resourceGroupsList)
            {
                string responseBodyRGResources = await Helper.GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resourceGroups/" + resourceGroup + "/resources?api-version=2017-05-10", token);

                string bodyRG = Helper.FormatResponse(responseBodyRGResources);

                await Helper.OutputCloudAsync(blobClient, containerName, "ResourceGroups/" + resourceGroup + "/" + dateFormatted + "-" + resourceGroup, bodyRG);
            }
        }
    }
}
