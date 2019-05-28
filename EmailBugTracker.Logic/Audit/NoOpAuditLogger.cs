using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic.Audit
{
    public class NoOpAuditLogger : IAuditLogger
    {
        public Task LogAsync(string eventName, Action<Dictionary<string, string>> dict = null)
        {
            return Task.CompletedTask;
        }
    }
}
