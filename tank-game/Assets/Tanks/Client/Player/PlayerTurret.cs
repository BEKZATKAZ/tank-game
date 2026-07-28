using System;
using Tanks.Client.Input;
using Tanks.Client.Rendering;
using Tanks.Shared;
using Tanks.Shared.Driving;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Player
{
    public sealed class PlayerTurret : NetworkComponent<TankTurretBehaviour>
    {
        [Inject] private readonly IInputService<TurretInput> _input;
        [Inject] private readonly ICameraMainService _camera;

        private float2 _lastDirection;

        private float2 CalculateFromPointer(float2 pointerPosition)
        {
            UnityCamera camera = (UnityCamera)_camera.Get();
            if (camera is null) return _lastDirection;

            float3 pointerPosition3d = new float3(pointerPosition.xy, 0);
            float2 worldPosition = ((float3)camera.OriginalCamera.ScreenToWorldPoint(pointerPosition3d)).xy;

            float2 playerPosition = Behaviour.Transform.WorldPosition;
            float2 direction = math.normalizesafe(worldPosition - playerPosition);

            return CalculateFromDirection(direction);
        }

        private float2 CalculateFromDirection(float2 direction)
        {
            if (math.lengthsq(direction) > 0.01f) _lastDirection = math.normalizesafe(direction);
            return _lastDirection;
        }

        private void Update()
        {
            TurretInput input = _input.Read();

            float2 direction = _input.CurrentScheme switch
            {
                InputScheme.KeyboardAndMouse => CalculateFromPointer(input.PointerPosition),
                InputScheme.Gamepad => CalculateFromPointer(input.LookAtDirection),
                _ => throw new NotImplementedException()
            };

            Behaviour.Tick(direction, Time.deltaTime);
        }

        private void Start()
        {
            if (Behaviour.IsLocalPlayer == false) enabled = false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
