using System;
using Tanks.Client.Input;
using Tanks.Client.Rendering;
using Tanks.Shared;
using Tanks.Shared.Driving;
using Unity.Mathematics;

namespace Tanks.Client.Player
{
    public sealed class PlayerTurret : NetworkComponent<ITankTurretBehaviour>
    {
        public PlayerTurret(IInputService<TurretInput> input, ICameraMainService camera)
        {
            _input = input;
            _camera = camera;
        }

        private readonly IInputService<TurretInput> _input;
        private readonly ICameraMainService _camera;

        private float2 _lastDirection;

        private float2 CalculateFromPointer(float2 pointerPosition)
        {
            ICamera camera = _camera.Get();
            if (camera is null) return _lastDirection;

            float2 worldPosition = camera.ScreenToWorldPoint(pointerPosition);
            float2 playerPosition = Transform.WorldPosition;
            float2 direction = math.normalizesafe(worldPosition - playerPosition);

            return NormalizeDirectionOrKeepLast(direction);
        }

        private float2 NormalizeDirectionOrKeepLast(float2 direction)
        {
            if (math.lengthsq(direction) > 0.01f) _lastDirection = math.normalizesafe(direction);
            return _lastDirection;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_input.IsEnabled == false) return;
            TurretInput input = _input.Read();

            float2 direction = _input.CurrentScheme switch
            {
                InputScheme.KeyboardAndMouse => CalculateFromPointer(input.PointerPosition),
                InputScheme.Gamepad => NormalizeDirectionOrKeepLast(input.LookAtDirection),
                _ => throw new NotImplementedException()
            };

            Behaviour.Tick(direction, deltaTime);
        }

        protected override void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer == false) IsEnabled = false;
        }
    }
}
