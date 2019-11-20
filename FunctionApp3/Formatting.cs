using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace FunctionApp3
{
    public class Formatting
    {
        // include 'dateFull.Substring(17, 2)' to specify seconds (use 11, 2 for hours and 14, 2 for mins) so that fileName is unique when running frequently for testing,
        // above is true only after the 10th of Oct, Nov, Dec; indices vary as single digit days/months are formatted as 'm/d/yyyy' not '0m/0d/yyyy' in Azure Function Apps, ie 1/1/2020 not 01/01/2020
        // formated as dd-mm-yyyy in Azure Function App. The DateTime struct returns differently formatted result in Visual Studio, Azure CLI and Azure Function Apps :@
        public static string FormatDateTime()
        {
            string dateFull = DateTime.Now.ToString();
            string dateFormatted = "";
            if (dateFull.Substring(1, 1) == "/" && dateFull.Substring(3, 1) == "/")
            {
                dateFormatted = "0" + dateFull.Substring(2, 1) + "-" + "0" + dateFull.Substring(0, 1) + "-" + dateFull.Substring(4, 4);
            }
            else if (dateFull.Substring(1, 1) == "/" && dateFull.Substring(4, 1) == "/")
            {
                dateFormatted = dateFull.Substring(2, 2) + "-" + "0" + dateFull.Substring(0, 1) + "-" + dateFull.Substring(5, 4);
            }
            else if (dateFull.Substring(2, 1) == "/" && dateFull.Substring(4, 1) == "/")
            {
                dateFormatted = "0" + dateFull.Substring(3, 1) + "-" + dateFull.Substring(0, 2) + "-" + dateFull.Substring(5, 4);
            }
            else if (dateFull.Substring(2, 1) == "/" && dateFull.Substring(5, 1) == "/")
            {
                dateFormatted = dateFull.Substring(3, 2) + "-" + dateFull.Substring(0, 2) + "-" + dateFull.Substring(6, 4);
            }
            return dateFormatted;
        }

        // request response (json parsed to string) is formated one line per resource, will be writen to a .csv file so ',' added as delimiter
        public static string FormatResponse(string responseBody, string subId)
        {
            string body = "Subscription ID\n" + subId + "\n\nResource Group,Name,Type,Other(s)\n";
            int start = 0;
            int count = 0;
            for (int i = 15; i < responseBody.Length - 1; i++)
            {
                try
                {
                    // cut the resourceGroupName out of the Id and add it to body
                    if (responseBody.Substring(i - 15, 15) == "resourceGroups/")
                    {
                        start = i;
                    }
                    if (responseBody.Substring(i - 10, 10) == "/providers")
                    {
                        int end = i - 10;
                        body += responseBody.Substring(start, end - start) + ",";
                    }
                    if (responseBody.Substring(i, 1) == "," && responseBody.Substring(i + 1, 1) != "{" && count < 3)
                    {
                        // skip Id as already have resourceGroupName
                        if (count == 0)
                        {
                            start = i + 9;
                            count++;
                        }
                        // add resourceName to body 
                        else if (count == 1)
                        {
                            body += responseBody.Substring(start, i - start - 1) + ",";
                            start = i + 9;
                            count++;
                        }
                        // add resourceType to body
                        else if (count == 2)
                        {
                            body += responseBody.Substring(start, i - start - 1) + ",";
                            start = i + 1;
                            count++;
                        }
                    }
                    // upon reaching the end of a resource add all other info (unorganised) and start a new line
                    if (responseBody.Substring(i, 2) == ",{")
                    {
                        body += responseBody.Substring(start, i - start - 1) + "\n";
                        start = i + 8;
                        count = 0;
                    }
                    // upon reaching the end of all resources add all other info (unorganised) for the last resource
                    if (responseBody.Substring(i, 1) == "]")
                    {
                        body += responseBody.Substring(start, i - start - 1);
                    }
                }
                catch (ArgumentOutOfRangeException outOfRange)
                {

                    Console.WriteLine("Error: {0} format", outOfRange.Message);
                }
            }
            return body;
        }

        // Overload of above, for resources by resource group
        public static string FormatResponse(string responseBody, string resourceGroup, string subId)
        {
            string body = "Subscription ID,Reasource Group\n" + subId + "," + resourceGroup + "\n\nName,Type,Other(s)\n";
            int start = 0;
            int count = 0;
            for (int i = 0; i < responseBody.Length - 1; i++)
            {
                try
                {
                    if (responseBody.Substring(i, 1) == "," && responseBody.Substring(i + 1, 1) != "}" && count < 3)
                    {
                        // skip Id as already no need for resourceGroupName
                        if (count == 0)
                        {
                            start = i + 9;
                            count++;
                        }
                        // add resourceName to body 
                        else if (count == 1)
                        {
                            body += responseBody.Substring(start, i - start - 1) + ",";
                            start = i + 9;
                            count++;
                        }
                        // add resourceType to body
                        else if (count == 2)
                        {
                            body += responseBody.Substring(start, i - start - 1) + ",";
                            start = i + 1;
                            count++;
                        }
                    }
                    // upon reaching the end of a resource add all other info (unorganised) and start a new line
                    if (responseBody.Substring(i, 2) == ",{")
                    {
                        body += responseBody.Substring(start, i - start - 1) + "\n";
                        start = i + 8;
                        count = 0;
                    }
                    // upon reaching the end of all resources add all other info (unorganised) for the last resource to body
                    if (responseBody.Substring(i, 1) == "]")
                    {
                        body += responseBody.Substring(start, i - start - 1);
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
