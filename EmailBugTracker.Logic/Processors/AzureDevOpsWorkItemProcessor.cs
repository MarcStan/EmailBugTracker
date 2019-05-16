using System;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic.Processors
{
    public class AzureDevOpsWorkItemProcessor : IWorkItemProcessor
    {
        private readonly WorkItemConfig _config;

        public AzureDevOpsWorkItemProcessor(WorkItemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task ProcessWorkItemAsync(WorkItem workItem)
        {
        }
    }
}
