using EmailBugTracker.Logic.Config;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic.Http
{
    public class HttpClient : IHttpClient
    {
        private readonly System.Net.Http.HttpClient _client;

        public HttpClient(System.Net.Http.HttpClient client, KeyvaultConfig config)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", config.WorkItemPAT))));
        }

        public Task<HttpResponseMessage> PostAsync(string url, string content, string contentType = "application/json-patch+json")
        {
            return _client.PostAsync(url, new StringContent(content, Encoding.Default, contentType));
        }
    }
}
