using System.Net.Http;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic.Http
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> PostAsync(string url, string content, string contentType = "application/json-patch+json");
    }
}
