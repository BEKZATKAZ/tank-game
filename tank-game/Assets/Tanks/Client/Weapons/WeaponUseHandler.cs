using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons;
using Unity.Collections;

namespace Tanks.Client.Weapons
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ScheduledClientShot
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ScheduledClientShot(int tick, ShotId id, Angle direction)
        {
            Tick = tick;
            Id = id;
            Direction = direction;
        }

        public readonly int Tick;
        public readonly ShotId Id;
        public readonly Angle Direction;
    }

    public sealed class WeaponUseHandler :
        NetworkComponent<IWeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsedFromServer>
    {
        private NativeQueue<ScheduledClientShot> _scheduledShots;

        public void Handle(WeaponUsedFromServer message)
        {
            _scheduledShots.Enqueue(new ScheduledClientShot(message.ServerTick, message.Id, message.Direction));
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            float affectedTickDelta =
                tickDelta * Behaviour.GetDeltaMultiplier();

            if (_scheduledShots.Count == 0)
            {
                Behaviour.WeaponTick(
                    affectedTickDelta,
                    isHoldingDown: false,
                    default
                );

                return;
            }

            while (_scheduledShots.Count > 0)
            {
                ScheduledClientShot shot = _scheduledShots.Peek();

                if (tick < shot.Tick)
                    break;

                Behaviour.WeaponTick(
                    affectedTickDelta,
                    isHoldingDown: true,
                    shot.Id,
                    shot.Direction
                );

                _scheduledShots.Dequeue();
            }

            if (_scheduledShots.Count == 0)
            {
                Behaviour.WeaponTick(
                    affectedTickDelta,
                    isHoldingDown: false,
                    default
                );
            }
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
