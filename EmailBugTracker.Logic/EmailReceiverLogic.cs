using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic
{
    public class EmailReceiverLogic
    {
        private readonly ITelemetry _telemetry;
        private readonly IWorkItemProcessor _workitemProcessor;

        public EmailReceiverLogic(
            IWorkItemProcessor workitemProcessor,
            ITelemetry telemetry)
        {
            _workitemProcessor = workitemProcessor ?? throw new ArgumentNullException(nameof(workitemProcessor));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public async Task RunAsync(KeyvaultConfig cfg, Stream body)
        {
            SendgridParameters param;
            using (var reader = new StreamReader(body))
            {
                param = JsonConvert.DeserializeObject<SendgridParameters>(await reader.ReadToEndAsync());
                Validate(param);
            }
            if (!IsWhitelisted(param.From, cfg.WhitelistedSenders))
            {
                _telemetry.TrackEvent("Non-whitelisted sender", dict => dict["sender"] = EmailAnonymization.PseudoAnonymize(param.From));
                return;
            }
            if (!IsWhitelisted(param.To, cfg.AllowedRecipients))
            {
                _telemetry.TrackEvent("Non-whitelisted recipient", dict => dict["recipient"] = EmailAnonymization.PseudoAnonymize(param.To));
                return;
            }

            var workItem = Parse(param);
            await _workitemProcessor.ProcessWorkItemAsync(workItem);
            _telemetry.TrackEvent("Work item created", dict =>
            {
                dict["title"] = workItem.Title;
                dict["sender"] = EmailAnonymization.PseudoAnonymize(param.From);
            });
        }

        private bool IsWhitelisted(string from, string whitelistedSenders)
        {
            if (string.IsNullOrEmpty(whitelistedSenders))
                return true;

            var allowedEmails = whitelistedSenders.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (allowedEmails.Any(e => e.Equals(from, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        private void Validate(SendgridParameters param)
        {
            if (string.IsNullOrEmpty(param.From))
                throw new ArgumentNullException(nameof(param.From));

            if (string.IsNullOrEmpty(param.To))
                throw new ArgumentNullException(nameof(param.To));

            if (string.IsNullOrEmpty(param.Subject))
                throw new ArgumentNullException(nameof(param.Subject));
        }

        private static WorkItem Parse(SendgridParameters param)
        {
            return new WorkItem
            {
                Title = param.Subject,
                Content = param.Html ?? "No content"
            };
        }
    }
}
