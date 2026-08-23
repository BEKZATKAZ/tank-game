using Tanks.Client.Input;
using Tanks.Shared;
using Tanks.Shared.Driving;
using Unity.Netcode.Components;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Player
{
    [RequireComponent(typeof(NetworkTransform), typeof(NetworkRigidbody2D), typeof(Rigidbody2D))]
    public sealed class ManualPlayerDrive : NetworkComponent<TankDriveBehaviour>
    {
        [Inject] private readonly IInputService<ManualDriveInput> _input;

        protected override void OnUpdate(float deltaTime)
        {
            ManualDriveInput input = _input.Read();
            Behaviour.Tick(input.Throttle, input.Steering, Time.deltaTime);
        }

        protected override void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer == false) IsEnabled = false;
        }
    }
}
