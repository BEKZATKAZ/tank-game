using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tanks.Shared.Weapons;
using Unity.Collections;
using Unity.Mathematics;

namespace Tanks.Server.Weapons
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ScheduledShot
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ScheduledShot(int tick, ShotId id)
        {
            Tick = tick;
            Id = id;
        }

        public readonly int Tick;
        public readonly ShotId Id;
    }

    public sealed class WeaponInputScheduler : IDisposable
    {
        public WeaponInputScheduler(Func<ScheduledShot, float, bool> shoot, Action<float> fallback)
        {
            _scheduledShots = new NativeQueue<ScheduledShot>(Allocator.Persistent);
            _processor = new WeaponInputProcessor();

            _onShoot = shoot;
            _fallback = fallback;
        }

        public int LastProcessedClientTick { get; private set; }
        public int ScheduledCount => _scheduledShots.Count;

        private readonly WeaponInputProcessor _processor;
        private readonly Func<ScheduledShot, float, bool> _onShoot;
        private readonly Action<float> _fallback;
        private readonly NativeQueue<ScheduledShot> _scheduledShots;

        private int _lastScheduledClientTick = -1;
        private int _lastScheduledServerTick = -1;

        public void Dispose()
        {
            _scheduledShots.Dispose();
        }

        public void Tick(int tick, float tickDelta)
        {
            if (_scheduledShots.Count == 0)
            {
                _fallback?.Invoke(tickDelta);
                return;
            }

            ScheduledShot shot = _scheduledShots.Peek();

            if (tick < shot.Tick)
            {
                _fallback?.Invoke(tickDelta);
                return;
            }

            _onShoot?.Invoke(shot, tickDelta);
            _scheduledShots.Dequeue();
        }

        public void Schedule(
            IReadOnlyWeapon weapon, WeaponInput input, int tick, float tickDelta)
        {
            float cooldownDuration = weapon.Cooldown.DurationSeconds;
            int cooldownTicks = (int)math.round(cooldownDuration / tickDelta);

            _processor.Process(
                input: input,
                lastProcessedTick: LastProcessedClientTick,
                callback: (shot) =>
                {
                    int scheduledTick;

                    if (_lastScheduledClientTick == -1) scheduledTick = tick;
                    else
                    {
                        // Discard too quick shots
                        int cooldowned = _lastScheduledServerTick + cooldownTicks;
                        scheduledTick = math.max(tick, cooldowned);
                    }

                    _lastScheduledClientTick = shot.ClientTick;
                    _lastScheduledServerTick = scheduledTick;
                    _scheduledShots.Enqueue(new ScheduledShot(scheduledTick, shot.Id));
                });

            LastProcessedClientTick = input.ClientTick;
        }
    }
}
