using System;
using Tanks.Core.Helpers;
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
        public RegeneratingAmmo(
            int current,
            int max,
            PositiveFloatOrZero regenerationDuration,
            PositiveFloatOrZero regenerationDelay) : base(current)
        {
            Max = max;
            RegenerationDuration = regenerationDuration;
            RegenerationDelay = regenerationDelay;
        }

        public int Max { get; }
        public PositiveFloatOrZero RegenerationDuration { get; }
        public PositiveFloatOrZero RegenerationDelay { get; }

        private PositiveFloatOrZero _regenerationElapsed;

        public override void Tick(float deltaTime)
        {
            if (Current >= Max) return;

            _regenerationElapsed += deltaTime;

            if (_regenerationElapsed <= RegenerationDelay) return;

            float time = _regenerationElapsed - RegenerationDelay;
            float alpha = math.saturate(time / RegenerationDuration);
            Current = (int)(alpha * Max);
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