using EmailBugTracker.Logic;
using EmailBugTracker.Logic.Config;
using EmailBugTracker.Logic.Http;
using EmailBugTracker.Logic.Processors;
using Microsoft.ApplicationInsights;
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
using System.Threading.Tasks;

namespace EmailBugTracker
{
    public static class Functions
    {
        [FunctionName("bugreport")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            var telemetryClient = new TelemetryClient();
            try
            {
                var config = LoadConfig(context.FunctionAppDirectory, log, telemetryClient);
                var telemetry = new Telemetry(telemetryClient);

                var workItemConfig = new WorkItemConfig();
                config.Bind(workItemConfig);

                var keyvaultConfig = new KeyvaultConfig();
                config.Bind(keyvaultConfig);

                var processor = new AzureDevOpsWorkItemProcessor(new HttpClient(new System.Net.Http.HttpClient(), keyvaultConfig), workItemConfig);
                var logic = new EmailReceiverLogic(processor, telemetry);

                var parser = new HttpFormDataParser(telemetry);
                var result = parser.Deserialize(req.Form);

                await logic.RunAsync(keyvaultConfig, result);
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);
                return new BadRequestResult();
            }
            return new OkResult();
        }

        private static void SetApplicationInsightsKeyIfExists(TelemetryClient telemetry, IConfiguration config, ILogger log)
        {
            var key = config["APPINSIGHTS_INSTRUMENTATIONKEY"];
            if (!string.IsNullOrEmpty(key))
                telemetry.InstrumentationKey = key;
            else
                log.LogWarning("Application insights key not set!");
        }

        /// <summary>
        /// Helper that loads the config values from file, environment variables and keyvault.
        /// </summary>
        private static IConfiguration LoadConfig(string workingDirectory, ILogger log, TelemetryClient telemetry)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                .SetBasePath(workingDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
                var tmpConfig = builder.Build();

                // build config from files only first
                // A) to get the keycault address..
                var keyvault = tmpConfig["KeyVaultName"];
                // B) to hook up AI for early error reporting
                SetApplicationInsightsKeyIfExists(telemetry, tmpConfig, log);

                var tokenProvider = new AzureServiceTokenProvider();
                var kvClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));
                builder.AddAzureKeyVault($"https://{keyvault}.vault.azure.net", kvClient, new DefaultKeyVaultSecretManager());

                var cfg = builder.Build();
                return cfg;
            }
            catch (Exception e)
            {
                telemetry.TrackException(e);
                log.LogCritical($"Failed accessing the keyvault: '{e.Message}'. Possible reason: You are debugging locally (in which case you must add your user account to the keyvault access policies manually). Note that the infrastructure deployment will reset the keyvault policies to only allow the azure function MSI! More details on local fallback here: https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication#local-development-authentication");
                throw;
            }
        }
    }
}
