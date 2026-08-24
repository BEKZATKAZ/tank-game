using Tanks.Client.Input;
using Tanks.Shared;
using Tanks.Shared.Weapons;
using Tanks.Utils;

namespace Tanks.Client.Weapons
{
    public sealed class WeaponFirePrediction : NetworkComponent<IWeaponHolderBehaviour>
    {
        public WeaponFirePrediction(IInputService<TurretInput> input, IShotIdGenerator idGenerator)
        {
            _input = input;
            _idGenerator = idGenerator;
        }

        private readonly IInputService<TurretInput> _input;
        private readonly IShotIdGenerator _idGenerator;

        private RingBuffer8<ClientShot> _fireBuffer;
        private bool _isHoldingDown;

        protected override void OnTick(int tick, float tickDelta)
        {
            ShotId shotId = _isHoldingDown
                ? _idGenerator.GenerateShotId(Behaviour.OwnerClientId, tick)
                : default;

            // Send nothing if the weapon was not used
            if (Behaviour.WeaponTick(tickDelta, _isHoldingDown, shotId) == false) return;

            // Send many inputs at once to avoid packet loss
            _fireBuffer = _fireBuffer.Add(new ClientShot(tick, shotId));
            Behaviour.SendInputServerRpc(new WeaponInput { FireBuffer = _fireBuffer });

            _isHoldingDown = false;
        }

        protected override void OnUpdate(float deltaTime)
        {
            _isHoldingDown = _input.Read().Fire;
        }

        protected override void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer == false) IsEnabled = false;
        }
    }
}
