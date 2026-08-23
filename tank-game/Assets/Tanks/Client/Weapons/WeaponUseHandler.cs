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

    public class WeaponUseHandler :
        NetworkComponent<WeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsedFromServer>
    {
        private NativeQueue<ScheduledClientShot> _scheduledShots;
        private int _lastScheduledTick;

        public void Handle(WeaponUsedFromServer message)
        {
            int currentTick = Behaviour.NetworkManager.NetworkTickSystem.LocalTime.Tick;
            float tickDelta = 1f / Behaviour.NetworkManager.NetworkTickSystem.TickRate;

            float cooldownDuration = Behaviour.CurrentWeapon.Cooldown.DurationSeconds;
            int cooldownTicks = (int)math.round(cooldownDuration / tickDelta);
            int cooldowned = _lastScheduledTick + cooldownTicks;

            _lastScheduledTick = math.max(currentTick, cooldowned);
            _scheduledShots.Enqueue(new ScheduledClientShot(_lastScheduledTick, message.Id));
        }

        protected override void OnTick(int tick, float tickDelta)
        {
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
