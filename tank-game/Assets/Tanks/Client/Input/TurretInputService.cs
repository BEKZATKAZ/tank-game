using Tanks.Client.Input.Bindings;
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
        public TurretInputService(IControlBindingService bindings = null)
            : base(bindings) { }

        protected override void EnableMaps() => Controls.Turret.Enable();

        public override TurretInput Read()
        {
            if (IsEnabled == false || IsSuspended) return default;

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
