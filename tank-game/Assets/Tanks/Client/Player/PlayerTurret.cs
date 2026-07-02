using Tanks.Client.Input;
using Tanks.Shared;
using Tanks.Shared.Driving;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Player
{
    public sealed class PlayerTurret : NetworkComponent<TankTurretBehaviour>
    {
        [Inject] private readonly IInputService<TurretInput> _input;

        private void Update()
        {
            TurretInput input = _input.Read();
            Behaviour.Tick(input.LookAtDirection, Time.deltaTime);
        }

        protected override void Awake()
        {
            base.Awake();
            if (Behaviour.IsOwner == false) enabled = false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}