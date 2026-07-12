using Tanks.Shared;
using Tanks.Shared.Weapons;

namespace Tanks.Server.Weapons
{
    public sealed class WeaponUserAuthority :
        NetworkComponent<WeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponInput>
    {
        private WeaponInputScheduler _scheduler;
        private WeaponInput? _latestUnprocessedInput;

        public void Handle(WeaponInput message)
        {
            if (_scheduler.LastProcessedClientTick >= message.ClientTick) return;

            if (_latestUnprocessedInput.HasValue &&
                _latestUnprocessedInput.Value.ClientTick > message.ClientTick) return;

            _latestUnprocessedInput = message;
        }

        public override void OnTick(int tick, float tickDelta)
        {
            if (_latestUnprocessedInput.HasValue)
            {
                WeaponInput input = _latestUnprocessedInput.Value;
                _scheduler.Schedule(Behaviour.CurrentWeapon, input, tick, tickDelta);
                _latestUnprocessedInput = null;
            }

            _scheduler.Tick(tick, tickDelta);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _scheduler.Dispose();
        }

        protected override void Awake()
        {
            base.Awake();

            _scheduler = new WeaponInputScheduler(
                shoot: (shot, deltaTime) => Behaviour.WeaponTick(deltaTime, true, shot.Id),
                fallback: deltaTime => Behaviour.WeaponTick(deltaTime, false, default));

            Behaviour.RegisterMessageHandler(this);
        }
    }
}
