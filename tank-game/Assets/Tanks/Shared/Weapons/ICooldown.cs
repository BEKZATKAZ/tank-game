using Tanks.Core.Helpers;
using Unity.Mathematics;

namespace Tanks.Shared.Weapons
{
    public interface IReadOnlyCooldown
    {
        float DurationSeconds { get; }

        bool CanUse();
    }

    public interface ICooldown : IReadOnlyCooldown
    {
        void Use();
        void Tick(float deltaTime);
    }

    public sealed class Cooldown : ICooldown
    {
        public Cooldown(PositiveFloat durationSeconds)
        {
            DurationSeconds = durationSeconds;
            Elapsed = durationSeconds.Value;
        }

        public PositiveFloatOrZero Elapsed { get; private set; }
        public float DurationSeconds { get; }

        public bool CanUse() => Elapsed >= DurationSeconds - math.EPSILON;
        public void Use() => Elapsed = 0;
        public void Tick(float deltaTime) => Elapsed += deltaTime;
    }

    public sealed class NoCooldown : ICooldown
    {
        public float DurationSeconds => 0;

        public bool CanUse() => true;
        public void Use() { }
        public void Tick(float deltaTime) { }
    }
}
