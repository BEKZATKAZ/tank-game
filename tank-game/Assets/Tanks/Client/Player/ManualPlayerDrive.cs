using Tanks.Client.Input;
using Tanks.Client.Input.Driving;
using Tanks.Shared;
using Tanks.Shared.Driving;
using Unity.Netcode.Components;
using UnityEngine;

namespace Tanks.Client.Player
{
    [RequireComponent(typeof(NetworkTransform), typeof(NetworkRigidbody2D), typeof(Rigidbody2D))]
    public sealed class ManualPlayerDrive : NetworkComponent<ITankDriveBehaviour>
    {
        public ManualPlayerDrive(
            IInputService<ManualDriveInput> input,
            DirectionalDriveResolverService resolver,
            DriveCommandService commands)
        {
            _input = input;
            _resolver = resolver;
            _commands = commands;
        }

        private readonly IInputService<ManualDriveInput> _input;
        private readonly DirectionalDriveResolverService _resolver;
        private readonly DriveCommandService _commands;

        protected override void OnUpdate(float deltaTime)
        {
            ManualDriveInput input = _input.Read();
            if (input.IsModern)
            {
                input = _resolver.Resolve(
                    input.Direction, Transform.WorldRotation.Degrees,
                    input.TurnInPlace, input.Reverse, deltaTime, Behaviour.TurnRate);
            }

            _commands.Current = input;
            Behaviour.Tick(input.Throttle, input.Steering, deltaTime);
        }

        protected override void OnDisable()
        {
            if (Behaviour.IsLocalPlayer) _commands.Current = default;
        }

        protected override void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer == false) IsEnabled = false;
        }
    }
}
