using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic.Audit
{
    public class BlobStorageAuditLogger : IAuditLogger
    {
        private readonly CloudBlobClient _client;
        private readonly string _containerName;

        public BlobStorageAuditLogger(string connectionString, string containerName)
        {
            var storageAccount = StorageAccount.NewFromConnectionString(connectionString);
            _client = storageAccount.CreateCloudBlobClient();
            _containerName = containerName;
        }

        public async Task LogAsync(string eventName, Action<Dictionary<string, string>> dict = null)
        {
            var d = DateTimeOffset.UtcNow;
            var blob = await GetBlobAsync($"{d.ToString("yyyy-MM-dd-HH-mm-ss")}_{eventName}.json");
            var data = new Dictionary<string, string>();
            dict?.Invoke(data);
            await blob.UploadTextAsync(JsonConvert.SerializeObject(new
            {
                eventType = eventName,
                data
            }, Formatting.Indented));
        }

        private async Task<CloudBlockBlob> GetBlobAsync(string blobName)
        {
            var container = _client.GetContainerReference(_containerName);
            await container.CreateIfNotExistsAsync();
            return container.GetBlockBlobReference(blobName);
        }
    }
}
