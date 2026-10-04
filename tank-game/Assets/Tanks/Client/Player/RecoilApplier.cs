using Tanks.Shared;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Unity.Mathematics;

namespace Tanks.Client.Player
{
    public sealed class RecoilApplier : 
        NetworkComponent<IWeaponHolderBehaviour>, INetworkMessageHandler<WeaponUsed>
    {
        private IStatOwnerBehaviour _statOwner;
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

            float amount = config.Amount - _statOwner.GetStat(StatType.Resistance, 0);
            if (amount <= 0) return;

            _longitudinalForce = -amount * forward * Transform.Right;
            _lateralForce = -amount * sideways * Transform.Up;
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

            _statOwner = Behaviour.GetBehaviour<IStatOwnerBehaviour>();
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
