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
            Vector2 turn = Controls.Turret.Turn.ReadValue<Vector2>();

            return new TurretInput
            {
                LookAtDirection = turn,
                PointerPosition = Controls.Turret.PointerPosition.ReadValue<Vector2>(),
                Fire = Controls.Turret.Fire.IsPressed()
            };
        }
    }
}
