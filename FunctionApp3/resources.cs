using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;

namespace FunctionApp3
{
    public static class Resources
    {
        [FunctionName("Resources")]
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
            // must be lowercase
            string containerName = "resources-sv";

            // reformat DateTime.Now, as / and : not usable in filenames
            string dateFormatted = Formatting.FormatDateTime();

            // aquire bearer token for the service principle
            string token = await InOutput.AquireBearerTokenAsync(tenantId, clientId, clientKey);

            // define Azure Stroage Account for outputs
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=" + storageName + ";AccountKey=" + storageKey + ";EndpointSuffix=core.windows.net";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // create and upload All Resources file
            string responseBody = await InOutput.GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resources?api-version=2017-05-10", token);
            string body = Formatting.FormatResponse(responseBody, subId);
            await InOutput.OutputCloudAsync(blobClient, containerName, dateFormatted + "/" + "AllResources.csv", body);

            // create and upload Resources files by resource group
            string responseBodyRG = await InOutput.GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resourceGroups?api-version=2014-04-01", token);
            List<string> resourceGroupsList = Formatting.ListResourceGroups(responseBodyRG, log);
            foreach (string resourceGroup in resourceGroupsList)
            {
                string responseBodyRGResources = await InOutput.GetInfoAsync("https://management.azure.com/subscriptions/" + subId + "/resourceGroups/" + resourceGroup + "/resources?api-version=2017-05-10", token);
                string bodyRG = Formatting.FormatResponse(responseBodyRGResources, resourceGroup, subId);
                await InOutput.OutputCloudAsync(blobClient, containerName, dateFormatted + "/" + resourceGroup + ".csv", bodyRG);
            }
        }
    }
}
