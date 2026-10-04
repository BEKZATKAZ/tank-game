using Tanks.Shared;
using Tanks.Shared.Weapons;

namespace Tanks.Server.Weapons
{
    public class WeaponUserAuthority :
        NetworkComponent<IWeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponInput>
    {
        public WeaponInputScheduler Scheduler { get; private set; }

        private WeaponInput? _latestUnprocessedInput;

        public void Handle(WeaponInput message)
        {
            if (Scheduler.LastProcessedClientTick >= message.ClientTick) return;

            if (_latestUnprocessedInput.HasValue &&
                _latestUnprocessedInput.Value.ClientTick > message.ClientTick) return;

            _latestUnprocessedInput = message;
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            if (_latestUnprocessedInput.HasValue)
            {
                WeaponInput input = _latestUnprocessedInput.Value;
                Scheduler.Schedule(Behaviour, input, tick, tickDelta);
                _latestUnprocessedInput = null;
            }

            Scheduler.Tick(tick, tickDelta * Behaviour.GetDeltaMultiplier());
        }

        protected override void OnDestroy()
        {
            Scheduler.Dispose();
        }

        protected override void OnInitialize()
        {
            Scheduler = new WeaponInputScheduler(
                shoot: (shot, deltaTime) => Behaviour.WeaponTick(deltaTime, true, shot.Id),
                fallback: deltaTime => Behaviour.WeaponTick(deltaTime, false, default));

            Behaviour.RegisterMessageHandler(this);
        }
    }
}
