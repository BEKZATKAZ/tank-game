using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tanks.Shared;
using Tanks.Shared.Weapons;
using Unity.Collections;
using Unity.Mathematics;

namespace Tanks.Client.Weapons
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ScheduledClientShot
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ScheduledClientShot(int tick, ShotId id)
        {
            Tick = tick;
            Id = id;
        }

        public readonly int Tick;
        public readonly ShotId Id;
    }

    public sealed class WeaponUseHandler :
        NetworkComponent<IWeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsedFromServer>
    {
        private NativeQueue<ScheduledClientShot> _scheduledShots;
        private int _lastScheduledTick;
        private int _currentTick;
        private float _tickDelta;

        public void Handle(WeaponUsedFromServer message)
        {
            float cooldownDuration = Behaviour.CurrentWeapon.Cooldown.DurationSeconds;
            int cooldownTicks = (int)math.round(cooldownDuration / _tickDelta);
            int cooldowned = _lastScheduledTick + cooldownTicks;

            _lastScheduledTick = math.max(_currentTick, cooldowned);
            _scheduledShots.Enqueue(new ScheduledClientShot(_lastScheduledTick, message.Id));
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            (_currentTick, _tickDelta) = (tick, tickDelta);

            if (_scheduledShots.Count == 0)
            {
                Behaviour.WeaponTick(tickDelta, isHoldingDown: false, default);
                return;
            }

            ScheduledClientShot shot = _scheduledShots.Peek();

            if (tick < shot.Tick)
            {
                Behaviour.WeaponTick(tickDelta, isHoldingDown: false, default);
                return;
            }

            Behaviour.WeaponTick(tickDelta, isHoldingDown: true, shot.Id);
            _scheduledShots.Dequeue();
        }

        protected override void OnDestroy()
        {
            if (_scheduledShots.IsCreated) _scheduledShots.Dispose();
        }

        protected override void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer)
            {
                IsEnabled = false;
                return;
            }

            _scheduledShots = new NativeQueue<ScheduledClientShot>(Allocator.Persistent);
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
