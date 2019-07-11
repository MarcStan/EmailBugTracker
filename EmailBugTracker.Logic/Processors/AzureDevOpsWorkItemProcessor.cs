using EmailBugTracker.Logic.Audit;
using EmailBugTracker.Logic.Config;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic.Processors
{
    public class AzureDevOpsWorkItemProcessor : IWorkItemProcessor
    {
        private readonly HttpClient _httpClient;
        private readonly WorkItemConfig _config;
        private readonly IAuditLogger _auditLogger;

        public AzureDevOpsWorkItemProcessor(HttpClient httpClient, WorkItemConfig config, IAuditLogger auditLogger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        }

        public async Task ProcessWorkItemAsync(WorkItem workItem, CancellationToken cancellationToken)
        {
            var project = GetProject(workItem);
            workItem.Metadata["project"] = project;

            // https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/work%20items/create?view=azure-devops-rest-5.0
            var json = JsonConvert.SerializeObject(new[]
            {
                // https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/work-item-field?view=azure-devops
                new
                {
                    op = "add",
                    path = "/fields/System.Title",
                    value = workItem.Title
                },
                new
                {
                    op = "add",
                    path = "/fields/Microsoft.VSTS.TCM.ReproSteps",
                    value = workItem.Content
                }
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");
            var response = await _httpClient.PostAsync($"https://dev.azure.com/{_config.Organization}/{project}/_apis/wit/workitems/$Bug?api-version=5.0", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            await _auditLogger.LogAsync("bug", dict =>
            {
                dict["sender"] = workItem.Metadata["sender"];
                dict["recipient"] = workItem.Metadata["recipient"];
                dict["title"] = workItem.Title;
                dict["content"] = workItem.Content;
            });
        }

        /// <summary>
        /// Tries to get the target project name from the work item.
        /// Fallback chain:
        /// 1. configured project
        /// 2. recipient address
        /// 3. prefix of title (will be removed from created workitem)
        /// 4. first word of title (will be kept in title)
        /// </summary>
        /// <param name="workItem"></param>
        private string GetProject(WorkItem workItem)
        {
            string project = _config.Project ?? "";
            if (!string.IsNullOrWhiteSpace(project))
                return project;

            if (_config.DetermineTargetProjectVia.HasFlag(DetermineTargetProjectVia.Recipient))
            {
                // fallback to recipient, e.g. project@example.com -> project
                if (workItem.Metadata.ContainsKey("recipient"))
                {
                    project = workItem.Metadata["recipient"];
                    var idx = project.IndexOf("@");
                    if (idx > -1)
                        project = project.Substring(0, idx);
                }
            }
            if (!string.IsNullOrEmpty(project))
                return project;

            if (_config.DetermineTargetProjectVia.HasFlag(DetermineTargetProjectVia.Subject))
            {
                // try to determine from subject
                var regex = new Regex(@"(\[.+?\])|(\(.+?\))|(.+?)\||(.+?)-");
                var match = regex.Match(workItem.Title);
                if (match.Success)
                {
                    // may receive multiple matches.
                    // e.g. "proj |" matches both the first and last group
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        var next = match.Groups[i].Value.Trim();
                        if (next.Length > project.Length)
                            project = next;
                    }
                    if (!string.IsNullOrEmpty(project))
                    {
                        workItem.Title = workItem.Title.Substring(project.Length).TrimStart("|-()[] \t".ToCharArray());
                        project = project.TrimStart("([".ToCharArray()).TrimEnd("])".ToCharArray());
                    }
                }
            }
            if (!string.IsNullOrEmpty(project))
                return project;

            // fallback to first word of title
            var first = workItem.Title.Split(' ').FirstOrDefault();
            return (first ?? "unknown bug title").Trim();
        }
    }
}
