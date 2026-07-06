using Tanks.Core.Helpers;

namespace Tanks.Shared.Weapons
{
    public interface IReadOnlyCooldown
    {
        bool CanUse();
    }

    public interface ICooldown : IReadOnlyCooldown
    {
        void Use();
        void Tick(float deltaTime);
    }

    public sealed class Cooldown : ICooldown
    {
        public Cooldown(PositiveFloatOrZero durationSeconds)
        {
            _durationSeconds = durationSeconds;
            _elapsed = durationSeconds;
        }

        private readonly PositiveFloatOrZero _durationSeconds;
        private PositiveFloatOrZero _elapsed;
        
        public bool CanUse() => _elapsed >= _durationSeconds;
        public void Use() => _elapsed = 0;
        public void Tick(float deltaTime) => _elapsed += deltaTime;
    }

    public sealed class NoCooldown : ICooldown
    {
        public bool CanUse() => true;
        public void Use() { }
        public void Tick(float deltaTime) { }
    }
}