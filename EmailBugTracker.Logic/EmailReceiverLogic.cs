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

        public EmailReceiverLogic(ITelemetry telemetry)
        {
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

            var workitem = Parse(param);
            _telemetry.TrackEvent("Work item created", dict =>
            {
                dict["title"] = workitem.Title;
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

        private static Workitem Parse(SendgridParameters param)
        {
            return new Workitem
            {
                Title = param.Subject,
                Content = param.Html
            };
        }

        public class Workitem
        {
            public string Title { get; set; }

            public string Content { get; set; }
        }
    }
}
