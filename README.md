# AzureResourcesFunctionApp
Timer triggered function app to get a list of all resources in an Azure subscription.

When triggered a text file is outputed to an Azure blob storage account, with one resource per line for ease of comparison.

The intent is to create a log of resources so if issues occur can easily review changes made to Azure resources to help diagnose.

Use:
```
diff --unified file1.txt file2.txt
```
to compare, lines prefixed with - indicate deletions, + for additions

### Environment Variables
define in the function app's application settings, use @Microsoft.KeyVault(SecretUri=SECRET_IDENTIFIER) for the value to call to secrets stored in Azure key vault.
