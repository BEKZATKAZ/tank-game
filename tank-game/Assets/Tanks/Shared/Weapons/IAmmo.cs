using System;
using Tanks.Core.Helpers;
using Tanks.Shared.Weapons.Data;
using Unity.Mathematics;

namespace Tanks.Shared.Weapons
{
    public interface IReadOnlyAmmo
    {
        ushort Current { get; }
        ushort Max { get; }

        bool CanConsume(ushort amount = 1);
    }

    public interface IAmmo : IReadOnlyAmmo
    {
        void Consume(ushort amount = 1);
        void Tick(float deltaTime);
    }

    public class FixedAmmo : IAmmo
    {
        public FixedAmmo(ushort current)
        {
            Current = current;
            Max = current;
        }

        public ushort Current { get; protected set; }
        public ushort Max { get; }

        public bool CanConsume(ushort amount = 1) => Current - amount >= 0;

        public virtual void Consume(ushort amount = 1)
        {
            if (amount <= 0 || amount > Current) throw new ArgumentOutOfRangeException();
            Current -= amount;
        }

        public virtual void Tick(float deltaTime) { }
    }

    public sealed class RegeneratingAmmo : FixedAmmo
    {
        public RegeneratingAmmo(IAmmoConfig config) : base(config.DefaultAmount)
        {
            _config = config;
            _regenerationStartAmount = Current;
        }

        private readonly IAmmoConfig _config;
        private PositiveFloatOrZero _regenerationElapsed;
        private ushort _regenerationStartAmount;

        public override void Tick(float deltaTime)
        {
            if (Current >= _config.MaxAmount) return;

            _regenerationElapsed += deltaTime;

            if (_regenerationElapsed <= _config.RegenerationDelay) return;

            float regenTime = _regenerationElapsed - _config.RegenerationDelay;
            float alpha = math.saturate(regenTime / _config.RegenerationDuration);

            Current = (ushort)math.min(_config.MaxAmount,
                _regenerationStartAmount + _config.MaxAmount * alpha);
        }

        public override void Consume(ushort amount = 1)
        {
            base.Consume(amount);
            _regenerationElapsed = 0;
            _regenerationStartAmount = Current;
        }
    }

    public sealed class InfiniteAmmo : IAmmo
    {
        public ushort Current => 1;
        public ushort Max => 1;

        public bool CanConsume(ushort amount = 1) => true;
        public void Consume(ushort amount = 1) { }
        public void Tick(float deltaTime) { }
    }
}
