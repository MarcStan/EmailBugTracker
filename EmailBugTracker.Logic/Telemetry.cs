using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;

namespace EmailBugTracker.Logic
{
    public class Telemetry : ITelemetry
    {
        private readonly TelemetryClient _telemetry;

        public Telemetry(TelemetryClient telemetry)
        {
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public void TrackEvent(string eventName, Action<Dictionary<string, string>> dict = null)
        {
            Dictionary<string, string> properties = null;
            if (dict != null)
            {
                properties = new Dictionary<string, string>();
                dict(properties);
            }
            _telemetry.TrackEvent(eventName, properties);
        }

        public void TrackException(Exception e)
           => _telemetry.TrackException(e);
    }
}
