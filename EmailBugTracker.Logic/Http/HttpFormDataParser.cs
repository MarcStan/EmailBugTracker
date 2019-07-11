using EmailBugTracker.Logic.Config;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;

namespace EmailBugTracker.Logic.Http
{
    public class HttpFormDataParser
    {
        private readonly ILogger _log;

        public HttpFormDataParser(ILogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public SendgridParameters Deserialize(IFormCollection form)
        {
            var param = new SendgridParameters();
            param.From = ParseEmail(form["from"]);
            param.To = ParseEmail(form["to"]);
            param.Subject = form["subject"];
            try
            {
                param.Content = ParseHtml(form["html"]);
            }
            catch (Exception e)
            {
                // fallback to raw html if parser fails
                _log.LogError(e, "Failed to deserialize form into email!");
                param.Content = form["text"];
                if (string.IsNullOrWhiteSpace(param.Content))
                    param.Content = form["html"];
            }

            return param;
        }

        private string ParseEmail(string email)
        {
            if (!email.Contains("<"))
            {
                // may just be a regular email
                if (email.Contains("@"))
                    return email; // good enough

                throw new NotSupportedException($"Invalid input received for display name. Expected 'some name <email>' but found: {email}");
            }

            email = email.Substring(email.LastIndexOf('<') + 1);
            if (email.Contains(">"))
                email = email.Substring(0, email.IndexOf('>'));
            return email.Trim();
        }

        private string ParseHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var signature = doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'Signature')]")?.InnerText;
            string text = doc.DocumentNode.SelectSingleNode("//body").InnerText;
            if (!string.IsNullOrEmpty(signature))
            {
                text = text.Replace(signature, "");
            }
            text = text.Trim();
            return text;
        }
    }
}
