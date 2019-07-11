using EmailBugTracker.Logic.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic
{
    public class EmailReceiverLogic
    {
        private readonly ILogger _log;
        private readonly IWorkItemProcessor _workitemProcessor;

        public EmailReceiverLogic(
            IWorkItemProcessor workitemProcessor,
            ILogger log)
        {
            _workitemProcessor = workitemProcessor ?? throw new ArgumentNullException(nameof(workitemProcessor));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task RunAsync(KeyvaultConfig cfg, SendgridParameters param, CancellationToken cancellationToken)
        {
            Validate(param);
            if (!IsWhitelisted(param.From, cfg.WhitelistedSenders))
            {
                _log.LogWarning($"Non-whitelisted sender: {EmailAnonymization.PseudoAnonymize(param.From)}");
                return;
            }
            if (!IsWhitelisted(param.To, cfg.AllowedRecipients))
            {
                _log.LogWarning($"Non-whitelisted recipient: {EmailAnonymization.PseudoAnonymize(param.To)}");
                return;
            }

            var workItem = Parse(param);
            await _workitemProcessor.ProcessWorkItemAsync(workItem, cancellationToken);
            var project = workItem.Metadata.ContainsKey("project") ? workItem.Metadata["project"] : "no project";
            _log.LogInformation($"Work item created: project: '{project}', title: '{workItem.Title}' by: {EmailAnonymization.PseudoAnonymize(param.From)}");
        }

        private bool IsWhitelisted(string from, string whitelistedSenders)
        {
            if (string.IsNullOrEmpty(whitelistedSenders))
                return true;

            var allowedEmails = whitelistedSenders.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            // reverse check to allow whitelisting entire domains as well
            // e.g. "sender@foo.com".Contains("@foo.com") -> true
            if (allowedEmails.Any(e => e.StartsWith("@") ?
            // domain filter "@example.com"
            from.EndsWith(e, StringComparison.OrdinalIgnoreCase) :
            // single email e.g. "foo@example.com" -> make sure that filter "o@example.com" doesn't match it
            e.Equals(from, StringComparison.OrdinalIgnoreCase)))
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
                Content = param.Content ?? "No content",
                Metadata = new Dictionary<string, string>
                {
                    {"recipient", param.To },
                    {"sender", param.From }
                }
            };
        }
    }
}
