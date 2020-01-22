using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace FunctionApp3
{
    public class Formatting
    {
        // include 'dateFull.Substring(17, 2)' to specify seconds (use 11, 2 for hours and 14, 2 for mins) so that fileName is unique when running frequently for testing,
        // above is true only after the 10th of Oct, Nov, Dec; indices vary as single digit days/months are formatted as 'm/d/yyyy' not '0m/0d/yyyy' in Azure Function Apps, ie 1/1/2020 not 01/01/2020
        // Output formated as yyyy/mm/dd in Azure Function App. The DateTime struct returns differently formatted result in Visual Studio, Azure CLI and Azure Function Apps :@
        public static string FormatDateTime()
        {
            string dateFull = DateTime.Now.ToString();
            string dateFormatted = "";
            if (dateFull.Substring(1, 1) == "/" && dateFull.Substring(3, 1) == "/")
            {
                //dateFormatted = "0" + dateFull.Substring(2, 1) + "-" + "0" + dateFull.Substring(0, 1) + "-" + dateFull.Substring(4, 4);
                dateFormatted = dateFull.Substring(4, 4) + "/" + "0" + dateFull.Substring(0, 1) + "/" + "0" + dateFull.Substring(2, 1);
            }
            else if (dateFull.Substring(1, 1) == "/" && dateFull.Substring(4, 1) == "/")
            {
                //dateFormatted = dateFull.Substring(2, 2) + "-" + "0" + dateFull.Substring(0, 1) + "-" + dateFull.Substring(5, 4);
                dateFormatted = dateFull.Substring(5, 4) + "/" + "0" + dateFull.Substring(0, 1) + "/" + dateFull.Substring(2, 2);
            }
            else if (dateFull.Substring(2, 1) == "/" && dateFull.Substring(4, 1) == "/")
            {
                //dateFormatted = "0" + dateFull.Substring(3, 1) + "-" + dateFull.Substring(0, 2) + "-" + dateFull.Substring(5, 4);
                dateFormatted = dateFull.Substring(5, 4) + "/" + dateFull.Substring(0, 2) + "/" + "0" + dateFull.Substring(3, 1);
            }
            else if (dateFull.Substring(2, 1) == "/" && dateFull.Substring(5, 1) == "/")
            {
                //dateFormatted = dateFull.Substring(3, 2) + "-" + dateFull.Substring(0, 2) + "-" + dateFull.Substring(6, 4);
                dateFormatted = dateFull.Substring(6, 4) + "/" + dateFull.Substring(0, 2) + "/" + dateFull.Substring(3, 2);
            }
            return dateFormatted;
        }

        // request response (json parsed to string) is formated one line per resource, will be writen to a .csv file so ',' added as delimiter
        public static List<string> FormatResponse(string responseBody)
        {
            List<string> ids = new List<string>();
            int start = 0;
            bool vm = false;
            for (int i = 0; i < responseBody.Length - 17; i++)
            {
                try
                {
                    if (responseBody.Substring(i, 5) == "\"id\":")
                    {
                        start = i + 7;
                        vm = false;
                    }
                    if (responseBody.Substring(i, 17) == "/virtualMachines/")
                    {
                        vm = true;
                    }
                    if (responseBody.Substring(i, 12) == "/extensions/")
                    {
                        vm = false;
                    }
                    if (responseBody.Substring(i, 8) == ",\"name\":" && vm)
                    {
                        ids.Add(responseBody.Substring(start, i - 1 - start));
                    }
                }
                catch (ArgumentOutOfRangeException outOfRange)
                {
                    Console.WriteLine("Error: {0} format", outOfRange.Message);
                }
            }
            return ids;
        }
        
        // create a List containing name of each resource group in the target subscription
        public static List<string> ListResourceGroups(string responseBodyRG, ILogger log)
        {
            List<string> resourceGroupsList = new List<string>();
            int start = 0;
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
                        int end = i - 3;
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
