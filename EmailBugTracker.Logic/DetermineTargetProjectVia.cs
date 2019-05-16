using System;

namespace EmailBugTracker.Logic
{
    [Flags]
    public enum DetermineTargetProjectVia
    {
        Unknown = 0,
        Recipient = 1,
        Subject = 2,
        All = Recipient | Subject
    }
}
