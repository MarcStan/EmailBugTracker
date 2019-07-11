using EmailBugTracker.Logic;
using EmailBugTracker.Logic.Audit;
using EmailBugTracker.Logic.Config;
using EmailBugTracker.Logic.Processors;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EmailBugTracker.Tests
{
    public class AzureDevOpsWorkItemProcessorTests
    {
        [Test]
        public async Task ResolvingProjectFromConfigShouldWork()
        {
            const string org = "org";
            const string proj = "proj";

            var expected = $"https://dev.azure.com/{org}/{proj}/_apis/wit/workitems/$Bug?api-version=5.0";
            var client = CreateClient(_ =>
            {
                if (_.RequestUri.ToString() != expected)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("ok"),
                };
            });

            var config = new WorkItemConfig
            {
                Organization = org,
                Project = proj,
                DetermineTargetProjectVia = DetermineTargetProjectVia.All
            };
            var audit = new Mock<IAuditLogger>();
            var processor = new AzureDevOpsWorkItemProcessor(client, config, audit.Object);

            await processor.ProcessWorkItemAsync(new WorkItem
            {
                Title = "foo",
                Content = "bar"
            }, CancellationToken.None);
            audit.Verify(x => x.LogAsync("bug", It.IsAny<Action<Dictionary<string, string>>>()), Times.Once);
        }

        [Test]
        public async Task ResolvingProjectFromEmailShouldWork()
        {
            const string org = "org";
            const string proj = "proj";

            var expected = $"https://dev.azure.com/{org}/{proj}/_apis/wit/workitems/$Bug?api-version=5.0";
            var client = CreateClient(_ =>
            {
                if (_.RequestUri.ToString() != expected)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("ok"),
                };
            });

            var config = new WorkItemConfig
            {
                Organization = org,
                Project = null,
                DetermineTargetProjectVia = DetermineTargetProjectVia.All
            };
            var audit = new Mock<IAuditLogger>();
            var processor = new AzureDevOpsWorkItemProcessor(client, config, audit.Object);

            await processor.ProcessWorkItemAsync(new WorkItem
            {
                Title = "foo",
                Content = "bar",
                Metadata = new Dictionary<string, string>
                {
                    { "recipient", proj + "@example.com" }
                }
            }, CancellationToken.None);
            audit.Verify(x => x.LogAsync("bug", It.IsAny<Action<Dictionary<string, string>>>()), Times.Once);
        }

        [Test]
        public async Task ResolvingProjectFromRecipientShouldTakePrecedenceOverSubject()
        {
            const string org = "org";

            var expected = $"https://dev.azure.com/{org}/project/_apis/wit/workitems/$Bug?api-version=5.0";
            var client = CreateClient(_ =>
            {
                if (_.RequestUri.ToString() != expected)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("ok"),
                };
            });

            var config = new WorkItemConfig
            {
                Organization = org,
                Project = null,
                DetermineTargetProjectVia = DetermineTargetProjectVia.All
            };
            var audit = new Mock<IAuditLogger>();
            var processor = new AzureDevOpsWorkItemProcessor(client, config, audit.Object);

            var item = new WorkItem
            {
                Title = "foobar",
                Content = "bar",
                Metadata = new Dictionary<string, string>
                {
                    { "recipient", "project@example.com" }
                }
            };
            await processor.ProcessWorkItemAsync(item, CancellationToken.None);
            audit.Verify(x => x.LogAsync("bug", It.IsAny<Action<Dictionary<string, string>>>()), Times.Once);
        }

        [TestCase("proj", "proj", "proj")]
        [TestCase("proj | foobar", "proj", "foobar")]
        [TestCase("proj - foobar", "proj", "foobar")]
        [TestCase("[proj] - foobar", "proj", "foobar")]
        [TestCase("(proj) - foobar", "proj", "foobar")]
        [TestCase("(proj)- foobar", "proj", "foobar")]
        public async Task ResolvingProjectFromSubjectShouldWork(string subject, string expectedProject, string modifiedSubject = null)
        {
            const string org = "org";
            modifiedSubject = modifiedSubject ?? subject;

            var expected = $"https://dev.azure.com/{org}/{expectedProject}/_apis/wit/workitems/$Bug?api-version=5.0";
            var client = CreateClient(_ =>
            {
                if (_.RequestUri.ToString() != expected)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("ok"),
                };
            });

            var config = new WorkItemConfig
            {
                Organization = org,
                Project = null,
                DetermineTargetProjectVia = DetermineTargetProjectVia.Subject
            };
            var audit = new Mock<IAuditLogger>();
            var processor = new AzureDevOpsWorkItemProcessor(client, config, audit.Object);

            var item = new WorkItem
            {
                Title = subject,
                Content = "bar"
            };
            await processor.ProcessWorkItemAsync(item, CancellationToken.None);
            audit.Verify(x => x.LogAsync("bug", It.IsAny<Action<Dictionary<string, string>>>()), Times.Once);
        }

        private HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> func)
        {
            return new HttpClient(new FuncHandler(func));
        }

        public class FuncHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _func;

            public FuncHandler(Func<HttpRequestMessage, HttpResponseMessage> func)
            {
                _func = func ?? throw new ArgumentNullException(nameof(func));
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_func(request));
            }
        }
    }
}
