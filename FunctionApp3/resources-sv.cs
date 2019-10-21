using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;

namespace FunctionApp3
{
    public static class Function1
    {
        [FunctionName("resources-sv")]
        public static async Task RunAsync([TimerTrigger("0 0 4 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // aquire bearer token, variable given when creating service principal. Only needs to run once (token is valid for 1 year by default) but currently runs everytime
            string tenantId = Environment.GetEnvironmentVariable("tenantId");
            string clientId = Environment.GetEnvironmentVariable("appIdSV");
            string clientKey = Environment.GetEnvironmentVariable("appKeySV");
            string authContextURL = "https://login.windows.net/" + tenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId, clientKey);
            var result = await authenticationContext
                .AcquireTokenAsync("https://management.azure.com/", credential);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the token");
            }
            string token = result.AccessToken;

            // send a get request for resources, with bearer token in header
            HttpClient httpClient = new HttpClient();
            string subId = Environment.GetEnvironmentVariable("subIdSV");
            string URL = "https://management.azure.com/subscriptions/" + subId + "/resources?api-version=2017-05-10";
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpResponseMessage response = await httpClient.GetAsync(URL);
            string responseBody = await response.Content.ReadAsStringAsync();

            // request responce (parsed to string) is formated (one line per resource) and outputed to a file
            string dateFull = DateTime.Now.ToString();
            // reformat DateTime, as / and : not usable in filenames
            // include 'dateFull.Substring(17, 2)' to specify seconds (use 11, 2 for hours and 14, 2 for mins) when run. So that name is unique when running frequently for testing
            // formated as dd/mm/yyyy
            string date = dateFull.Substring(3, 2) + "-" + dateFull.Substring(0, 2) + "-" + dateFull.Substring(6, 4);
            //string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, $"Resources{date}.txt")))
            string body = "";
            //{
            int s = 1;
                for (int i = 0; i < responseBody.Length - 1; i++)
                {
                    try
                    {
                    
                        if (responseBody.Substring(i, 1) == "[")
                        {
                            //outputFile.WriteLine("[");
                            body += "[\n";
                            s = i + 1;
                        }
                        if (responseBody.Substring(i, 2) == ",{")
                        {
                            //outputFile.WriteLine(responseBody.Substring(s, i - s));
                            body += responseBody.Substring(s, i - s) + "\n";
                            s = i + 1;
                        }
                        if (responseBody.Substring(i, 1) == "]")
                        {
                            //outputFile.WriteLine(responseBody.Substring(s, i - s));
                            body += responseBody.Substring(s, i - s) + "\n";
                            //outputFile.WriteLine("]");
                            body += "]";
                        }
                    }
                    catch (ArgumentOutOfRangeException outOfRange)
                    {

                        Console.WriteLine("Error: {0}", outOfRange.Message);
                    }                  
                }
            //}
            //Upload a file to Azure blob
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=" + Environment.GetEnvironmentVariable("storageName") + ";AccountKey=" + Environment.GetEnvironmentVariable("storageKey") + ";EndpointSuffix=core.windows.net";
            string name = "resources-sv";
            //string body = "test info";
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a created container.
            CloudBlobContainer container = blobClient.GetContainerReference(name);

            // Create the container if it does not already exist.
            bool result2 = await container.CreateIfNotExistsAsync();
            if (result2 == true)
            {
                log.LogInformation("Created container {0}", container.Name);
            }

            // Upload file with name "date" and content "body"
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(date);
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
            {
                log.LogInformation("streaming : ");
                await blockBlob.UploadFromStreamAsync(stream);
            }
        }
    }
}
