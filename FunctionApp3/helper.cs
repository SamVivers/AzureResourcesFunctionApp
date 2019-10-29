using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

public class Helper
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="URL"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<string> GetInfoAsync(string URL, string token)
    {
        // send a get request for resources, with bearer token in header
        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Remove("Authorization");
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        HttpResponseMessage response = await httpClient.GetAsync(URL);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Formats the response by turning the Json format into a easier to read format
    /// request response (json parsed to string) is formated one line per resource
    /// Out of bound exception within a try and catch statement.
    /// </summary>
    /// <param name="responseBody"></param>
    /// <returns></returns>
    public static string FormatResponse(string responseBody)
    {
        // 
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

    /// <summary>
    /// Creates the folder in MyDocuments
    /// Printing the result from the body string to MyDocuments 
    /// </summary>
    /// <param name="body"></param>
    /// <param name="date"></param>
    public static void OutputLocal(string body, string date)
    {
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, $"Resources{date}.txt")))
        {
            outputFile.Write(body);
        }
    }
    /// <summary>
    /// Creates the Container and Block Blob. 
    /// Printing the result from the body string to Azure Blob Container. 
    /// upload file, include / in fileName to create folders
    /// </summary>
    /// <param name="blobClient"></param>
    /// <param name="containerName"></param>
    /// <param name="fileName"></param>
    /// <param name="body"></param>
    /// <returns></returns>
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