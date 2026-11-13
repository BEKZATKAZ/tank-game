using Unity.Mathematics;

namespace Tanks.Client.Input
{
    public struct ManualDriveInput
    {
        public float Throttle;
        public float Steering;
        public float2 Direction;
        public bool IsModern;
        public bool TurnInPlace;
        public bool Reverse;
    }
}
