# Initial setup

## 1. Deploy infrastructure

First the infrastructure deployment is needed.

Build pipelines are provided and should set up the required infrastructure for you. You can run them with a free [Azure DevOps](https://azure.microsoft.com/services/devops/) account.

All that is needed is the resourcegroup name for the release. By convention all resources are named the same as the resourcegroup. Storage account will have any dashes removed automatically due to its name limitation.

Once all resources are deployed you should have the azure function, keyvault, app insights and storage ready to go.

The total cost will amount to a few cents per month since all these resources are pay-per-usage.

## 2. Filling the keyvault

You must manually insert the required secrets into the keyvault after it is deployed:

1. Secret `WorkItemPAT` must contain a PAT that has at least write permissions for work items.
2. Secret `AllowedRecipient` is optional. If set it contains the recipients that should be processed. 

    By default sendgrid forwards all emails but you may only want to open bugs for emails received at `bugs@example.com`. Multiple emails are allowed via `,;`seperators (`bugs@example.com;support@example.com,contact@example.com`).
3. Secret `WhitelistedSenders` is optional. If set, only senders in its list are allowed to report bugs that create bug items in Azure DevOps. 
    
    E.g. `me@example.com` prevents anyone whos sender is not `me@example.com` from creating bugs in Azure DevOps.

## 3. Code deployment

Once the infrastructure is in place you can deploy the azure function code. via the release.

## 4. Sendgrid setup

Azure provides a free sendgrid tier with 25000 emails/month. You can set it up for free and then follow the steps for [setting up Inbound Parse](https://sendgrid.com/docs/for-developers/parsing-email/inbound-email/).

The url you will have to enter is the url of your azure function.

You can get it from the [Azure portal](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function#test-the-function).

Note that the base url will not be enough as authentication is set to `Function`. This means you must provide the function key as well and the final url will look something like this: 

> `<ResourceGroupName>`.azurewebsites.net/api/bugreport?code=`<your-function-key-here>`
