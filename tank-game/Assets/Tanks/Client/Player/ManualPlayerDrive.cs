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

        private void Update()
        {
            ManualDriveInput input = _input.Read();
            Behaviour.Tick(input.Throttle, input.Steering, Time.deltaTime);
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
