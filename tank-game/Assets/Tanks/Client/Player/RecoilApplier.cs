using Tanks.Shared;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Unity.Mathematics;

namespace Tanks.Client.Player
{
    public sealed class RecoilApplier : 
        NetworkComponent<IWeaponHolderBehaviour>, INetworkMessageHandler<WeaponUsed>
    {
        private float _longitudinalElapsed;
        private float _lateralElapsed;
        private float2 _longitudinalForce;
        private float2 _lateralForce;
        private float _duration;

        public void Handle(WeaponUsed message)
        {
            IRecoilConfig config = message.Weapon.Value.Recoil;

            float forward = math.dot(Transform.Right, Behaviour.MuzzlePoint.Right);
            float sideways = math.dot(Transform.Up, Behaviour.MuzzlePoint.Right);
            float lateralCoeff = 0.5f;

            _longitudinalForce = -config.Amount * forward * Transform.Right;
            _lateralForce = -config.Amount * sideways * Transform.Up;
            _duration = config.DurationSeconds;

            _longitudinalElapsed = _duration;
            _lateralElapsed = math.abs(sideways) * lateralCoeff * _duration;
        }

        protected override void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer == false)
            {
                IsEnabled = false;
                return;
            }

            Behaviour.RegisterMessageHandler(this);
        }

        protected override void OnLateUpdate(float deltaTime)
        {
            if (_longitudinalElapsed <= 0 && _lateralElapsed <= 0) return;

            float longitudinal = _longitudinalElapsed / _duration;
            float lateral = _lateralElapsed / _duration;

            // Using LateUpdate to let the movement apply first
            // Only then apply recoil

            float2 recoil = longitudinal * _longitudinalForce + lateral * _lateralForce;
            Behaviour.Velocity.Linear += recoil;

            _longitudinalElapsed = math.max(_longitudinalElapsed - deltaTime, 0);
            _lateralElapsed = math.max(_lateralElapsed - deltaTime, 0);
        }
    }
}
