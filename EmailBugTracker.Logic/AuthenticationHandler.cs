using EmailBugTracker.Logic.Config;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic
{
    public class AuthenticationHandler : DelegatingHandler
    {
        private readonly KeyvaultConfig _config;

        public AuthenticationHandler(KeyvaultConfig config)
            : base(new HttpClientHandler())
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", _config.WorkItemPAT))));
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
