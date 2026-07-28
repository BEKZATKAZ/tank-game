using System;
using Tanks.Core.Helpers;
using Tanks.Shared.Weapons.Data;
using Unity.Mathematics;

namespace Tanks.Shared.Weapons
{
    public interface IReadOnlyAmmo
    {
        bool CanConsume(int amount = 1);
    }

    public interface IAmmo : IReadOnlyAmmo
    {
        void Consume(int amount = 1);
        void Tick(float deltaTime);
    }

    public class FixedAmmo : IAmmo
    {
        public FixedAmmo(int current) => Current = current;

        public int Current { get; protected set; }

        public bool CanConsume(int amount = 1) => Current - amount >= 0;

        public virtual void Consume(int amount = 1)
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
        }

        private readonly IAmmoConfig _config;
        private PositiveFloatOrZero _regenerationElapsed;

        public override void Tick(float deltaTime)
        {
            if (Current >= _config.MaxAmount) return;

            _regenerationElapsed += deltaTime;

            if (_regenerationElapsed <= _config.RegenerationDelay) return;

            float regenTime = _regenerationElapsed - _config.RegenerationDelay;
            float alpha = math.saturate(regenTime / _config.RegenerationDuration);

            int missing = _config.MaxAmount - Current;
            Current += (int)(missing * alpha);

            if (Current >= _config.MaxAmount) Current = _config.MaxAmount;
        }

        public override void Consume(int amount = 1)
        {
            base.Consume(amount);
            _regenerationElapsed = 0;
        }
    }

    public sealed class InfiniteAmmo : IAmmo
    {
        public bool CanConsume(int amount = 1) => true;
        public void Consume(int amount = 1) { }
        public void Tick(float deltaTime) { }
    }
}
