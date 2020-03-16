﻿using System;
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
        public static string FormatResponse(string responseBody, string subId, string resourceGroup = "All")
        {
            string body = "Subscription ID";
            int start = 0;
            int column = 0;
            int bracket = 1;
            if (resourceGroup == "All")
            {
                body += "\n" + subId + "\n\nResource Group,Name,Type,SKU,ManagedBy,Kind,Location,Identity,Plan,Tags\n\n";
            }
            else
            {
                body += ",Reasource Group\n" + subId + "," + resourceGroup + "\n\nName,Type,SKU,ManagedBy,Kind,Location,Identity,Plan,Tags\n\n";
            }
            for (int i = 15; i < responseBody.Length; i++)
            {
                try
                {
                    // cut the resourceGroupName out of the Id and add it to body && only do it once per resource (disks have a ManagedBy field which contains "resourceGroups/" and "/providers")
                    if (resourceGroup == "All")
                    {
                        if (responseBody.Substring(i - 15, 15) == "resourceGroups/")
                        {
                            start = i;
                        }
                        if (responseBody.Substring(i - 10, 10) == "/providers")
                        {
                            body += responseBody.Substring(start, i - 10 - start) + ",";
                            resourceGroup = "Done";
                        }
                    }
                    // some string we want to pick out as titles occur as keys within nested object, by counting brackets we can distinguish these 
                    if (responseBody.Substring(i, 1) == "{")
                    {
                        bracket++;
                    }
                    if (responseBody.Substring(i, 1) == "}")
                    {
                        bracket--;
                    }
                    // scan through the JSON for specific keys (translated to column headers) and fill out the column with info
                    if (responseBody.Substring(i - 7, 7) == "\"name\":" && bracket < 2)
                    {
                        start = i + 1;
                    }
                    if (responseBody.Substring(i - 7, 7) == "\"type\":" && bracket < 2)
                    {
                        body += responseBody.Substring(start, i - 9 - start) + ",";
                        start = i + 1;
                        column++;
                    }
                    // both 'name and 'type' will exist for all resources but for 'sku' onwards we may need to leave blank columns
                    string[] columnHeaders = {"\"sku\":", "\"managedBy\":", "\"kind\":", "\"location\":", "\"identity\":", "\"plan\":", "\"tags\":"};

                    foreach (string columnHeader in columnHeaders)
                    {
                        if (responseBody.Substring(i - columnHeader.Length, columnHeader.Length) == columnHeader)
                        {
                            body += responseBody.Substring(start, i - columnHeader.Length - 2 - start) + ",";
                            start = i + 1;
                            column++;
                            while (column < Array.IndexOf(columnHeaders, columnHeader) + 2)
                            {
                                body += ",";
                                column++;
                            }
                        }
                    }
                    // upon reaching the end of a resource add all tag info and start a new line
                    if (responseBody.Substring(i - 2, 2) == ",{")
                    {
                        body += responseBody.Substring(start, i - 4 - start) + "\n";
                        column = 0;
                        if (resourceGroup == "Done")
                        {
                            resourceGroup = "All";
                        }
                    }
                    // upon reaching the end of all resources add tag info for the last resource
                    if (responseBody.Substring(i - 1, 1) == "]")
                    {
                        body += responseBody.Substring(start, i - 1 - start);
                    }
                }
                catch (ArgumentOutOfRangeException outOfRange)
                {
                    Console.WriteLine("Error: {0} format", outOfRange.Message);
                }
            }
            // body will be written as a CSV file, however some fields (ie SKU) have commas seperating their "key":"value" pairs, we want these to be writen into the same column so commas are replaced by spaces
            string bodyEdit = "";
            int quotes = 0;
            start = 0;
            for (int i = 2; i < body.Length; i++)
            {
                try
                {
                    if (body.Substring(i - 2, 1) == "\"")
                    {
                        quotes++;
                    }

                    if (quotes == 4)
                    {
                        if (body.Substring(i - 1, 2) == ",\"")
                        {
                            bodyEdit += body.Substring(start, i - 1 - start) + " ";
                            start = i;
                        }
                        quotes = 0;
                    }
                    if (body.Substring(i, 1) == "}")
                    {
                        bodyEdit += body.Substring(start, i - 1 - start);
                    }
                }
                catch (ArgumentOutOfRangeException outOfRange)
                {
                    Console.WriteLine("Error: {0} format", outOfRange.Message);
                }
            }
            return bodyEdit;
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
