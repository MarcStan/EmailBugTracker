using EmailBugTracker.Logic.Config;
using EmailBugTracker.Logic.Http;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic.Processors
{
    public class AzureDevOpsWorkItemProcessor : IWorkItemProcessor
    {
        private readonly IHttpClient _httpClient;
        private readonly WorkItemConfig _config;

        public AzureDevOpsWorkItemProcessor(IHttpClient httpClient, WorkItemConfig config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task ProcessWorkItemAsync(WorkItem workItem)
        {
            var project = GetProject(workItem);
            workItem.Metadata["project"] = project;

            // https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/work%20items/create?view=azure-devops-rest-5.0
            var content = JsonConvert.SerializeObject(new[]
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
            var response = await _httpClient.PostAsync($"https://dev.azure.com/{_config.Organization}/{project}/_apis/wit/workitems/$Bug?api-version=5.0", content);
            response.EnsureSuccessStatusCode();
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
