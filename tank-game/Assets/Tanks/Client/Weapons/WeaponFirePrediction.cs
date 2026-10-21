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
        private bool _isUsing;
        private IReadOnlyWeapon _lastUsedWeapon;

        protected override void OnTick(int tick, float tickDelta)
        {
            ShotId shotId = _isHoldingDown
                ? _idGenerator.GenerateShotId(Behaviour.OwnerClientId, tick)
                : default;

            // Predict rate of fire
            float affectedTickDelta = tickDelta * Behaviour.GetDeltaMultiplier();

            // Send nothing if the weapon was not used
            if (Behaviour.WeaponTick(affectedTickDelta, _isUsing, shotId) == false) return;

            // Send many inputs at once to avoid packet loss
            _fireBuffer = _fireBuffer.Add(new ClientShot(tick, shotId));
            Behaviour.SendInputServerRpc(new WeaponInput { FireBuffer = _fireBuffer });

            _isHoldingDown = false;
        }

        protected override void OnUpdate(float deltaTime)
        {
            _isHoldingDown = _input.Read().Fire;

            // First update
            _lastUsedWeapon ??= Behaviour.CurrentWeapon;

            // Prevent using default weapon right after current weapon runs out
            if (_isUsing == false && _isHoldingDown == false)
            {
                _lastUsedWeapon = Behaviour.CurrentWeapon;
                _isUsing = false;
                return;
            }

            _isUsing = _isHoldingDown && _lastUsedWeapon == Behaviour.CurrentWeapon;
        }

        protected override void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer == false) IsEnabled = false;
        }
    }
}
