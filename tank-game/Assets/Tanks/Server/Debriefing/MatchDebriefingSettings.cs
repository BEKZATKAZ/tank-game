using System;

namespace Tanks.Server.Debriefing
{
    [Serializable]
    public sealed class MatchDebriefingSettings
    {
        public float SubmissionTimeoutSeconds = 30;
        public float AcknowledgmentTimeoutSeconds = 2;
    }
}
