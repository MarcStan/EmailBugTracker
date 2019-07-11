using EmailBugTracker.Logic;
using EmailBugTracker.Logic.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmailBugTracker.Tests
{
    public class EmailReceiverLogicTests
    {
        [Test]
        public async Task WhenSenderWhitelistIsUsedThenOtherSendersShouldBeDiscarded()
        {
            var logger = new Mock<ILogger>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, logger.Object);

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
            await logic.RunAsync(config, param, CancellationToken.None);

            Verify(logger, "Non-whitelisted sender", LogLevel.Warning);
        }

        [Test]
        public async Task WhenSenderWhitelistIsUsedWithADomainThenAllSendersOfTHeDomainShouldBeAllowed()
        {
            var logger = new Mock<ILogger>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, logger.Object);

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
            await logic.RunAsync(config, param, CancellationToken.None);

            Verify(logger, "Work item created", LogLevel.Information);
        }

        [Test]
        public async Task WhenSenderWhitelistIsUsedWithADomainThenOtherSendersShouldBeDiscarded()
        {
            var logger = new Mock<ILogger>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, logger.Object);

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
            await logic.RunAsync(config, param, CancellationToken.None);

            Verify(logger, "Non-whitelisted sender", LogLevel.Warning);
        }

        [Test]
        public async Task WhenSenderWhitelistIsNotUsedThenAnySendersShouldBeAllowed()
        {
            var logger = new Mock<ILogger>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, logger.Object);

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
            await logic.RunAsync(config, param, CancellationToken.None);

            Verify(logger, "Work item created", LogLevel.Information);
        }

        [Test]
        public async Task WhenRecipientWhitelistIsUsedThenOtherRecipientsShouldBeDiscarded()
        {
            var logger = new Mock<ILogger>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, logger.Object);

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
            await logic.RunAsync(config, param, CancellationToken.None);

            Verify(logger, "Non-whitelisted recipient", LogLevel.Warning);
        }

        [Test]
        public async Task WhenSenderWhitelistIsUsedWithDomainThenAnySenderOfTheDomainShouldBeAllowed()
        {
            var logger = new Mock<ILogger>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, logger.Object);

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
            await logic.RunAsync(config, param, CancellationToken.None);

            Verify(logger, "Work item created", LogLevel.Information);
        }

        [Test]
        public async Task WhenRecipientWhitelistWithDomainIsUsedThenOtherRecipientsShouldBeDiscarded()
        {
            var logger = new Mock<ILogger>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, logger.Object);

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
            await logic.RunAsync(config, param, CancellationToken.None);

            Verify(logger, "Non-whitelisted recipient", LogLevel.Warning);
        }

        [Test]
        public async Task WhenRecipientWhitelistIsNotUsedThenAnyRecipientShouldBeAllowed()
        {
            var logger = new Mock<ILogger>();
            var processor = new Mock<IWorkItemProcessor>();
            var logic = new EmailReceiverLogic(processor.Object, logger.Object);

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
            await logic.RunAsync(config, param, CancellationToken.None);

            Verify(logger, "Work item created", LogLevel.Information);
        }

        private void Verify(Mock<ILogger> logger, string v, LogLevel level)
        {
            logger.Verify(x => x.Log(level, It.IsAny<EventId>(), It.Is<object>(o => ((string)((FormattedLogValues)o)[0].Value).Contains(v)), null, It.IsAny<Func<object, Exception, string>>()));
        }
    }
}
