using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Input
{
    public struct TurretInput
    {
        public float2 LookAtDirection;
        public float2 PointerPosition;
        public bool Fire;
    }

    public sealed class TurretInputService : InputServiceBase<TurretInput>
    {
        public override TurretInput Read()
        {
            return new TurretInput
            {
                LookAtDirection = math.normalizesafe(Controls.Turret.Turn.ReadValue<Vector2>()),
                PointerPosition = Controls.Turret.Turn.ReadValue<Vector2>(),
                Fire = Controls.Turret.Fire.IsPressed()
            };
        }
    }
}
