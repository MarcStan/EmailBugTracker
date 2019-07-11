using EmailBugTracker.Logic;
using EmailBugTracker.Logic.Audit;
using EmailBugTracker.Logic.Config;
using EmailBugTracker.Logic.Http;
using EmailBugTracker.Logic.Processors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EmailBugTracker
{
    public static class Functions
    {
        [FunctionName("bugreport")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            Microsoft.Azure.WebJobs.ExecutionContext context,
            ILogger log,
            CancellationToken cancellationToken)
        {
            try
            {
                var config = LoadConfig(context.FunctionAppDirectory, log);

                var workItemConfig = new WorkItemConfig();
                config.Bind(workItemConfig);

                var keyvaultConfig = new KeyvaultConfig();
                config.Bind(keyvaultConfig);

                IAuditLogger auditLogger = new NoOpAuditLogger();
                if (!string.IsNullOrEmpty(workItemConfig.AuditContainerName))
                    auditLogger = new BlobStorageAuditLogger(config["AzureWebJobsStorage"], workItemConfig.AuditContainerName);

                var handler = new AuthenticationHandler(keyvaultConfig);
                var processor = new AzureDevOpsWorkItemProcessor(new HttpClient(handler), workItemConfig, auditLogger);
                var logic = new EmailReceiverLogic(processor, log);

                var parser = new HttpFormDataParser(log);
                var result = parser.Deserialize(req.Form);

                await logic.RunAsync(keyvaultConfig, result, cancellationToken);
            }
            catch (Exception e)
            {
                log.LogCritical(e, "Request failed!");
                return new BadRequestResult();
            }
            return new OkResult();
        }

        /// <summary>
        /// Helper that loads the config values from file, environment variables and keyvault.
        /// </summary>
        private static IConfiguration LoadConfig(string workingDirectory, ILogger log)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                .SetBasePath(workingDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
                var tmpConfig = builder.Build();

                // build config from files only first
                // to get the keycault address..
                var keyvault = tmpConfig["KeyVaultName"];

                var tokenProvider = new AzureServiceTokenProvider();
                var kvClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));
                builder.AddAzureKeyVault($"https://{keyvault}.vault.azure.net", kvClient, new DefaultKeyVaultSecretManager());

                var cfg = builder.Build();
                return cfg;
            }
            catch (Exception e)
            {
                log.LogCritical($"Failed accessing the keyvault: '{e.Message}'. Possible reason: You are debugging locally (in which case you must add your user account to the keyvault access policies manually). Note that the infrastructure deployment will reset the keyvault policies to only allow the azure function MSI! More details on local fallback here: https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication#local-development-authentication");
                throw;
            }
        }
    }
}
