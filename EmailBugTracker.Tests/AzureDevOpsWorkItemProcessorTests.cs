using EmailBugTracker.Logic;
using EmailBugTracker.Logic.Processors;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
            var http = new Mock<IHttpClient>();
            http.Setup(x => x.PostAsync(expected, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("ok"),
                }));

            var config = new WorkItemConfig
            {
                Organization = org,
                Project = proj,
                DetermineTargetProjectVia = DetermineTargetProjectVia.All
            };
            var processor = new AzureDevOpsWorkItemProcessor(http.Object, config);

            await processor.ProcessWorkItemAsync(new WorkItem
            {
                Title = "foo",
                Content = "bar"
            });
        }

        [Test]
        public async Task ResolvingProjectFromEmailShouldWork()
        {
            const string org = "org";
            const string proj = "proj";

            var expected = $"https://dev.azure.com/{org}/{proj}/_apis/wit/workitems/$Bug?api-version=5.0";
            var http = new Mock<IHttpClient>();
            http.Setup(x => x.PostAsync(expected, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("ok"),
                }));

            var config = new WorkItemConfig
            {
                Organization = org,
                Project = null,
                DetermineTargetProjectVia = DetermineTargetProjectVia.All
            };
            var processor = new AzureDevOpsWorkItemProcessor(http.Object, config);

            await processor.ProcessWorkItemAsync(new WorkItem
            {
                Title = "foo",
                Content = "bar",
                Metadata = new Dictionary<string, string>
                {
                    { "recipient", proj + "@example.com" }
                }
            });
        }

        [Test]
        public async Task ResolvingProjectFromRecipientShouldTakePrecedenceOverSubject()
        {
            const string org = "org";

            var expected = $"https://dev.azure.com/{org}/project/_apis/wit/workitems/$Bug?api-version=5.0";
            var http = new Mock<IHttpClient>();
            // ensure uri and title match in request
            http.Setup(x => x.PostAsync(expected, It.Is<string>(c => c.Contains("\"value\":\"foobar\"")), It.IsAny<string>()))
                .Returns(Task.FromResult(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("ok"),
                }));

            var config = new WorkItemConfig
            {
                Organization = org,
                Project = null,
                DetermineTargetProjectVia = DetermineTargetProjectVia.All
            };
            var processor = new AzureDevOpsWorkItemProcessor(http.Object, config);

            var item = new WorkItem
            {
                Title = "foobar",
                Content = "bar",
                Metadata = new Dictionary<string, string>
                {
                    { "recipient", "project@example.com" }
                }
            };
            await processor.ProcessWorkItemAsync(item);
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
            var http = new Mock<IHttpClient>();
            // ensure uri and title match in request
            http.Setup(x => x.PostAsync(expected, It.Is<string>(c => c.Contains($"\"value\":\"{modifiedSubject}\"")), It.IsAny<string>()))
                .Returns(Task.FromResult(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("ok"),
                }));

            var config = new WorkItemConfig
            {
                Organization = org,
                Project = null,
                DetermineTargetProjectVia = DetermineTargetProjectVia.Subject
            };
            var processor = new AzureDevOpsWorkItemProcessor(http.Object, config);

            var item = new WorkItem
            {
                Title = subject,
                Content = "bar"
            };
            await processor.ProcessWorkItemAsync(item);
        }
    }
}
