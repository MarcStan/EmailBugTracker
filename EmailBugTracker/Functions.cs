using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
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
            var config = LoadConfig(context.FunctionAppDirectory);
            SendgridParameters param;
            using (var reader = new StreamReader(req.Body))
            {
                param = JsonConvert.DeserializeObject<SendgridParameters>(await reader.ReadToEndAsync());
            }
            var cfg = new KeyvaultConfig();
            config.Bind(cfg);
            if (!string.IsNullOrEmpty(param.To) &&
                cfg.AllowedRecipient.Contains(param.To))
            {
                var workitem = Parse(param);
                log.LogInformation($"Work item {workitem.Title} would have been created");
            }
            return new OkResult();
        }

        private static Workitem Parse(SendgridParameters param)
        {
            return new Workitem
            {
                Title = param.Subject,
                Content = param.Html
            };
        }

        /// <summary>
        /// Helper that loads the config values from file and, environment variables.
        /// </summary>
        private static IConfiguration LoadConfig(string workingDirectory)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(workingDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            var tmpConfig = builder.Build();

            var keyvault = tmpConfig["KeyVaultName"];
            var tokenProvider = new AzureServiceTokenProvider();
            var kvClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));
            builder.AddAzureKeyVault($"https://{keyvault}.vault.azure.net", kvClient, new DefaultKeyVaultSecretManager());

            var cfg = builder.Build();
            return cfg;
        }

        public class Workitem
        {
            public string Title { get; set; }

            public string Content { get; set; }
        }
    }
}
