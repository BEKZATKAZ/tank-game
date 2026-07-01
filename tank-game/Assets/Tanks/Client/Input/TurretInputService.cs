using Tanks.Client.Rendering;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Input
{
    public struct TurretInput
    {
        public float2 LookAtDirection;
        public bool Fire;
    }

    public sealed class TurretInputService : InputServiceBase<TurretInput>
    {
        [Inject] private readonly ICameraMainService _camera;

        public override TurretInput Read()
        {
            return new TurretInput
            {
                LookAtDirection = CurrentScheme switch
                {
                    InputScheme.KeyboardAndMouse => GetLookAtDirectionAsMouse(),
                    InputScheme.Gamepad or _ => GetLookAtDirectionAsGamepad()
                },
                Fire = Controls.Turret.Fire.IsPressed()
            };
        }

        private float2 GetLookAtDirectionAsGamepad()
        {
            float2 direction = Controls.Turret.Turn.ReadValue<Vector2>();
            return math.normalizesafe(direction);
        }

        private float2 GetLookAtDirectionAsMouse()
        {
            UnityCamera camera = (UnityCamera)_camera.Get();
            if (camera is null) return float2.zero;

            float2 screenPos = Controls.Turret.Turn.ReadValue<Vector2>();
            float3 screenPos3D = new float3(screenPos.x, screenPos.y, 0);

            float2 worldPoint = ((float3)camera.OriginalCamera.ScreenToWorldPoint(screenPos3D)).xy;
            float2 thisPosition = camera.Transform.WorldPosition;

            return math.normalizesafe(worldPoint - thisPosition);
        }
    }
}