using Newtonsoft.Json;
using System;
using System.IO;
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
            }
            if (!string.IsNullOrEmpty(param.To) &&
                cfg.AllowedRecipients.Contains(param.To))
            {
                var workitem = Parse(param);
                _telemetry.TrackEvent("Workitem created", dict =>
                {
                    dict["title"] = workitem.Title;
                    dict["sender"] = param.From;
                });
            }
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
