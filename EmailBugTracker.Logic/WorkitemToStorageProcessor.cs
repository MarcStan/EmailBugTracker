using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic
{
    public class WorkItemToStorageProcessor : IWorkItemProcessor
    {
        private readonly CloudStorageAccount _account;

        public WorkItemToStorageProcessor(CloudStorageAccount account)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
        }

        public async Task ProcessWorkItemAsync(WorkItem workitem)
        {
            var client = _account.CreateCloudBlobClient();
            var container = client.GetContainerReference("test");
            await container.CreateIfNotExistsAsync();

            var now = DateTimeOffset.UtcNow;
            var blob = container.GetBlockBlobReference($"{now.ToString("yyyy-MM-dd-HH-mm-ss")}.json");
            using (var stream = await blob.OpenWriteAsync())
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(JsonConvert.SerializeObject(workitem));
            }
        }
    }
}
