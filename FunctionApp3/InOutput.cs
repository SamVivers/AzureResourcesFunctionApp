using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage.Blob;

namespace FunctionApp3
{
    class InOutput
    {
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

        // send a get request for resources, with bearer token in header
        public static async Task<string> GetInfoAsync(string URL, string token)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpResponseMessage response = await httpClient.GetAsync(URL);
            return await response.Content.ReadAsStringAsync();
        }

        // Output locally, for testing
        public static void OutputLocal(string body, string dateFormatted)
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, $"Resources{dateFormatted}.txt")))
            {
                outputFile.Write(body);
            }
        }

        // In Azure Stroage create container and upload file, include / in fileName to create folders
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
