using System;
using System.Collections.Generic;

namespace EmailBugTracker.Logic
{
    public interface ITelemetry
    {
        void TrackException(Exception e);

        void TrackEvent(string eventName, Action<Dictionary<string, string>> dict = null);
    }
}
