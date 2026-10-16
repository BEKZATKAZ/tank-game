using System;
using Unity.Mathematics;

namespace Tanks.Server.Generation.Crates
{
    [Serializable]
    public struct CrateGenerationSettings
    {
        public float ClusterDensity;
        public int2 MinMaxCratesPerCluster;
        public int ClusterRadius;
        public int MinClusterDistance;
        public float CornerBias;

        public int EpicCrateCount;
        public int EpicRadius;

        public float RareCrateDensity;
        public int MinSingleCrateDistance;
    }
}
