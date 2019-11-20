# AzureResourcesFunctionApp
Timer triggered function app to get a list of all resources in an Azure subscription.

When triggered files are outputed to an Azure blob storage account, with one resource per line for ease of comparison.

Creates a file with all resources listed and a also files for resources by resource group.

The intent is to create a log of resources so if issues occur can easily review changes made to Azure resources to help diagnose.

Use:
```
diff --unified file1 file2
```
to compare, lines prefixed with - indicate deletions, + for additions

### Service Principle
The app needs permissions to access the Azure subscription in the form of a service principle from the subscription to be polled

Use:
```
az ad sp create-for-rbac
```
in Azure CLI (of desired subscription), this will provide the tenantId, clientId and clientKey

NEED TO DECIDE WHICH ROLL (--roll) MEETS POLP!

### Environment Variables
defined in the function app's application settings and stored in an Azure Key Vault, use @Microsoft.KeyVault(SecretUri=SECRET_IDENTIFIER) for the value to call to secrets. You will need to assign the Function App an identity and allow this identity access to read secrets in the key vault.

tenantId, clientId and clientKey defined when creating service principle, as above.

subId is the subscription Id to be polled.

storageName and storageKey refer to the storage account that created files will be stored in.

containerName used to group data from same subscription. Could use the subId here, however a more readable name ie company or subscription owner is clearer
