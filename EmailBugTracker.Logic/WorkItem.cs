using System.Collections.Generic;

namespace EmailBugTracker.Logic
{
    public class WorkItem
    {
        public string Title { get; set; }

        public string Content { get; set; }

        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
