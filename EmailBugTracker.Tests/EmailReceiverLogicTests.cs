using EmailBugTracker.Logic;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmailBugTracker.Tests
{
    public class EmailReceiverLogicTests
    {
        [Test]
        public async Task WhenSenderWhitelistIsUsedThenOtherSendersShouldBeDiscarded()
        {
            var telemetry = new Mock<ITelemetry>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, telemetry.Object);

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
            await logic.RunAsync(config, param);

            telemetry.Verify(x => x.TrackEvent("Non-whitelisted sender", It.IsAny<Action<Dictionary<string, string>>>()));
        }

        [Test]
        public async Task WhenSenderWhitelistIsUsedWithADomainThenAllSendersOfTHeDomainShouldBeAllowed()
        {
            var telemetry = new Mock<ITelemetry>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, telemetry.Object);

            var config = new KeyvaultConfig
            {
                WhitelistedSenders = "@example.com",
                AllowedRecipients = "bugs@example.com"
            };
            var param = new SendgridParameters
            {
                From = "allowed@example.com",
                To = "bugs@example.com",
                Subject = "Test"
            };
            await logic.RunAsync(config, param);

            telemetry.Verify(x => x.TrackEvent("Work item created", It.IsAny<Action<Dictionary<string, string>>>()));
        }

        [Test]
        public async Task WhenSenderWhitelistIsUsedWithADomainThenOtherSendersShouldBeDiscarded()
        {
            var telemetry = new Mock<ITelemetry>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, telemetry.Object);

            var config = new KeyvaultConfig
            {
                WhitelistedSenders = "@example.com",
                AllowedRecipients = "bugs@example.com"
            };
            var param = new SendgridParameters
            {
                From = "notallowed@example2.com",
                To = "bugs@example.com",
                Subject = "Test"
            };
            await logic.RunAsync(config, param);

            telemetry.Verify(x => x.TrackEvent("Non-whitelisted sender", It.IsAny<Action<Dictionary<string, string>>>()));
        }

        [Test]
        public async Task WhenSenderWhitelistIsNotUsedThenAnySendersShouldBeAllowed()
        {
            var telemetry = new Mock<ITelemetry>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, telemetry.Object);

            var config = new KeyvaultConfig
            {
                WhitelistedSenders = null,
                AllowedRecipients = "bugs@example.com"
            };
            var param = new SendgridParameters
            {
                From = "unauthorized@example.com",
                To = "bugs@example.com",
                Subject = "Test"
            };
            await logic.RunAsync(config, param);

            telemetry.Verify(x => x.TrackEvent("Work item created", It.IsAny<Action<Dictionary<string, string>>>()));
        }

        [Test]
        public async Task WhenRecipientWhitelistIsUsedThenOtherRecipientsShouldBeDiscarded()
        {
            var telemetry = new Mock<ITelemetry>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, telemetry.Object);

            var config = new KeyvaultConfig
            {
                WhitelistedSenders = null,
                AllowedRecipients = "bugs@example.com"
            };
            var param = new SendgridParameters
            {
                From = "unauthorized@example.com",
                To = "notbugs@example.com",
                Subject = "Test"
            };
            await logic.RunAsync(config, param);

            telemetry.Verify(x => x.TrackEvent("Non-whitelisted recipient", It.IsAny<Action<Dictionary<string, string>>>()));
        }

        [Test]
        public async Task WhenSenderWhitelistIsUsedWithDomainThenAnySenderOfTheDomainShouldBeAllowed()
        {
            var telemetry = new Mock<ITelemetry>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, telemetry.Object);

            var config = new KeyvaultConfig
            {
                WhitelistedSenders = null,
                AllowedRecipients = "@example.com"
            };
            var param = new SendgridParameters
            {
                From = "allowed@example.com",
                To = "anything@example.com",
                Subject = "Test"
            };
            await logic.RunAsync(config, param);

            telemetry.Verify(x => x.TrackEvent("Work item created", It.IsAny<Action<Dictionary<string, string>>>()));
        }

        [Test]
        public async Task WhenRecipientWhitelistWithDomainIsUsedThenOtherRecipientsShouldBeDiscarded()
        {
            var telemetry = new Mock<ITelemetry>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, telemetry.Object);

            var config = new KeyvaultConfig
            {
                WhitelistedSenders = null,
                AllowedRecipients = "@example.com"
            };
            var param = new SendgridParameters
            {
                From = "unauthorized@example.com",
                To = "bugs@example2.com",
                Subject = "Test"
            };
            await logic.RunAsync(config, param);

            telemetry.Verify(x => x.TrackEvent("Non-whitelisted recipient", It.IsAny<Action<Dictionary<string, string>>>()));
        }

        [Test]
        public async Task WhenRecipientWhitelistIsNotUsedThenAnyRecipientShouldBeAllowed()
        {
            var telemetry = new Mock<ITelemetry>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, telemetry.Object);

            var config = new KeyvaultConfig
            {
                WhitelistedSenders = null,
                AllowedRecipients = null
            };
            var param = new SendgridParameters
            {
                From = "unauthorized@example.com",
                To = "someone@example.com",
                Subject = "Test"
            };
            await logic.RunAsync(config, param);

            telemetry.Verify(x => x.TrackEvent("Work item created", It.IsAny<Action<Dictionary<string, string>>>()));
        }
    }
}
