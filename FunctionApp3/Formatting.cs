using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace FunctionApp3
{
    public class Formatting
    {
        // include 'dateFull.Substring(17, 2)' to specify seconds (use 11, 2 for hours and 14, 2 for mins) so that fileName is unique when running frequently for testing,
        // above is true only after the 10th of Oct, Nov, Dec; indices vary as single digit days/months are formatted as 'n' not '0n' in Azure Function Apps, ie 1/1/2020 not 01/01/2020
        // formated as dd-mm-yyyy (or d/m/yyyy, d/mm/yyyy, dd/m/yyyy) in Azure Function App. The DateTime struct returns differently formatted result in Visual Studio, Azure CLI and Azure Function Apps :@
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
    }
}
