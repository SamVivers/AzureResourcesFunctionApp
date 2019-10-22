using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage.Blob;

public class Helper
{
    public static async Task<string> GetInfoAsync(string URL, string token)
    {
        // send a get request for all resources, with bearer token in header
        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Remove("Authorization");
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        HttpResponseMessage response = await httpClient.GetAsync(URL);
        return await response.Content.ReadAsStringAsync();
    }

    public static string FormatResponse(string responseBody)
    {

        // request responce (parsed to string) is formated (one line per resource) and outputed to a file
        string body = "";
        int start = 1;
        for (int i = 0; i<responseBody.Length - 1; i++)
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

                Console.WriteLine("Error: {0}", outOfRange.Message);
            }
        }
        return body;
    }

    public static void OutputLocal(string body, string date)
    {
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, $"Resources{date}.txt")))
        {
            outputFile.Write(body);
        }
    }
    public static async Task OutputCloudAsync(CloudBlobClient blobClient, string containerName, string fileName, string body)
    {
        // Create container and upload file
        CloudBlobContainer container = blobClient.GetContainerReference(containerName);
        CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
        using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
        {
            await blockBlob.UploadFromStreamAsync(stream);
        }
    }
}