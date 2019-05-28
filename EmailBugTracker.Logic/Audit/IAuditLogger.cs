using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmailBugTracker.Logic.Audit
{
    public interface IAuditLogger
    {
        Task LogAsync(string eventName, Action<Dictionary<string, string>> dict = null);
    }
}
