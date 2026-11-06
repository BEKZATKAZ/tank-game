using Tanks.Core.Settings;
using UnityEngine;

namespace Tanks.Infrastructure.Settings
{
    public sealed class UnityFrameRateController : IFrameRateController
    {
        public int TargetFrameRate
        {
            get => Application.targetFrameRate;
            set => Application.targetFrameRate = value;
        }

        public int VSyncCount
        {
            get => QualitySettings.vSyncCount;
            set => QualitySettings.vSyncCount = value;
        }
    }
}
