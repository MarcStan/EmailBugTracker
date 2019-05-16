using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using System;

namespace EmailBugTracker.Logic
{
    public class HttpFormDataParser
    {
        private readonly ITelemetry _telemetry;

        public HttpFormDataParser(ITelemetry telemetry)
        {
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
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
                _telemetry.TrackException(e);
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
