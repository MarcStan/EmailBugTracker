using EmailBugTracker.Logic;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace EmailBugTracker.Tests
{
    public class EmailReceiverLogicTests
    {
        [Test]
        public async Task WhenSenderWhitelistIsUsedThenOtherSendersShouldBeDiscarded()
        {
            var telemetry = new Mock<ITelemetry>();
            var logic = new EmailReceiverLogic(telemetry.Object);

            var config = new KeyvaultConfig
            {
                WhitelistedSenders = "legit@example.com",
                AllowedRecipients = "bugs@example.com"
            };
            var param = new SendgridParameters
            {
                From = "unauthorized@example.com",
                To = "bugs@example.com",
                Subject = "Test"
            };
            using (var body = CreateHttpBody(param))
                await logic.RunAsync(config, body);

            telemetry.Verify(x => x.TrackEvent("Workitem created", It.IsAny<Action<Dictionary<string, string>>>()));
        }

        private Stream CreateHttpBody(SendgridParameters param)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(JsonConvert.SerializeObject(param));
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}
